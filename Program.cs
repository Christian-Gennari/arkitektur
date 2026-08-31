using System.Text.Json;
using arkitektur.Application;
using arkitektur.Application.Events;
using arkitektur.Application.Handlers;
using arkitektur.Application.Interfaces;
using arkitektur.Infrastructure.Events;
using arkitektur.Infrastructure.Logging;
using arkitektur.Infrastructure.Repositories;
using arkitektur.Infrastructure.Simulation;
using arkitektur.Infrastructure.Statistics;
using arkitektur.Infrastructure.Tracking;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EventProcessingOptions>(
    builder.Configuration.GetSection(EventProcessingOptions.SectionName));
builder.Services.Configure<SubscriberSimulationOptions>(
    builder.Configuration.GetSection(SubscriberSimulationOptions.SectionName));
builder.Services.AddSingleton<SubscriberSimulationDelay>();
builder.Services.AddSingleton<EventMonitor>();
builder.Services.AddSingleton<EventBus>();
builder.Services.AddSingleton<IEventPublisher>(services => services.GetRequiredService<EventBus>());
builder.Services.AddHostedService(services => services.GetRequiredService<EventBus>());

builder.Services.AddSingleton<IShipmentRepository, InMemoryShipmentRepository>();
builder.Services.AddSingleton<ShipmentService>();
builder.Services.AddSingleton<IOperationsMetrics, OperationsMetrics>();
builder.Services.AddSingleton<ITrackingProjection, InMemoryTrackingProjection>();
builder.Services.AddSingleton<IPostalAuditLog, FilePostalAuditLog>();

builder.Services.AddSingleton<TrackingProjectionEventHandler>();
builder.Services.AddSingleton<CustomerNotificationEventHandler>();
builder.Services.AddSingleton<OperationsMetricsEventHandler>();
builder.Services.AddSingleton<PostalAuditEventHandler>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var bus = app.Services.GetRequiredService<EventBus>();
var trackingHandler = app.Services.GetRequiredService<TrackingProjectionEventHandler>();
var notificationHandler = app.Services.GetRequiredService<CustomerNotificationEventHandler>();
var metricsHandler = app.Services.GetRequiredService<OperationsMetricsEventHandler>();
var auditHandler = app.Services.GetRequiredService<PostalAuditEventHandler>();

Subscribe<ShipmentRegistered>(trackingHandler, notificationHandler, metricsHandler, auditHandler);
Subscribe<ShipmentDispatched>(trackingHandler, notificationHandler, metricsHandler, auditHandler);
Subscribe<ShipmentDelivered>(trackingHandler, notificationHandler, metricsHandler, auditHandler);
Subscribe<ShipmentCancelled>(trackingHandler, notificationHandler, metricsHandler, auditHandler);

void Subscribe<TEvent>(params IEventHandler<TEvent>[] handlers) where TEvent : IEvent
{
    foreach (var handler in handlers) bus.Subscribe(handler);
}

app.MapGet("/shipments", (ShipmentService service) => Results.Ok(service.GetAll()));

app.MapGet("/shipments/{id:int}", (int id, ShipmentService service) =>
    service.GetById(id) is { } shipment ? Results.Ok(shipment) : Results.NotFound());

app.MapPost("/shipments", async (RegisterShipmentRequest request, ShipmentService service) =>
{
    try
    {
        var shipment = await service.Register(request.Recipient, request.Destination);
        return Results.Created($"/shipments/{shipment.Id}", shipment);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(exception.Message);
    }
});

app.MapPut("/shipments/{id:int}/dispatch", async (int id, ShipmentService service) =>
    ToTransitionResult(await service.Dispatch(id)));

app.MapPut("/shipments/{id:int}/deliver", async (int id, ShipmentService service) =>
    ToTransitionResult(await service.Deliver(id)));

app.MapPut("/shipments/{id:int}/cancel", async (int id, ShipmentService service) =>
    ToTransitionResult(await service.Cancel(id)));

app.MapGet("/tracking/{trackingNumber}", (string trackingNumber, ITrackingProjection tracking) =>
    tracking.Get(trackingNumber) is { } snapshot ? Results.Ok(snapshot) : Results.NotFound());

app.MapGet("/operations/metrics", (IOperationsMetrics metrics) => Results.Ok(new
{
    metrics.RegisteredCount,
    metrics.DispatchedCount,
    metrics.DeliveredCount,
    metrics.CancelledCount,
}));

app.MapGet("/events/stream", async (HttpContext context, EventMonitor monitor) =>
{
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";
    context.Response.Headers.Append("X-Accel-Buffering", "no");
    context.Response.ContentType = "text/event-stream";
    var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    try
    {
        await foreach (var trace in monitor.Subscribe(context.RequestAborted))
        {
            var json = JsonSerializer.Serialize(trace, jsonOptions);
            await context.Response.WriteAsync(
                $"id: {trace.Sequence}\nevent: event-trace\ndata: {json}\n\n",
                context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
        }
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
        // EventSource reconnects automatically.
    }
});

app.Run();

static IResult ToTransitionResult(ShipmentTransitionResult result) => result switch
{
    ShipmentTransitionResult.Success => Results.NoContent(),
    ShipmentTransitionResult.NotFound => Results.NotFound(),
    _ => Results.Conflict("The shipment cannot make that transition from its current status."),
};

public sealed record RegisterShipmentRequest(string Recipient, string Destination);
