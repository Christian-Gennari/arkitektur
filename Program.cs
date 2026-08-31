using System.Text.Json;
using arkitektur.Application;
using arkitektur.Application.Events;
using arkitektur.Application.Handlers;
using arkitektur.Application.Interfaces;
using arkitektur.Domain.Models;
using arkitektur.Infrastructure.Events;
using arkitektur.Infrastructure.Logging;
using arkitektur.Infrastructure.Repositories;
using arkitektur.Infrastructure.Statistics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<EventProcessingOptions>(
    builder.Configuration.GetSection(EventProcessingOptions.SectionName)
);
builder.Services.AddSingleton<EventMonitor>();
builder.Services.AddSingleton<EventBus>();
builder.Services.AddSingleton<IEventPublisher>(services =>
    services.GetRequiredService<EventBus>());
builder.Services.AddHostedService(services => services.GetRequiredService<EventBus>());
builder.Services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();
builder.Services.AddSingleton<IActivityLogger, FileActivityLogger>();
builder.Services.AddSingleton<StatisticsEventHandler>();
builder.Services.AddSingleton<ActivityLogEventHandler>();
builder.Services.AddSingleton<TodoService>();
builder.Services.AddSingleton<IStatisticsService, StatisticsService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
var bus = app.Services.GetRequiredService<EventBus>();
var statisticsHandler = app.Services.GetRequiredService<StatisticsEventHandler>();
var activityLogHandler = app.Services.GetRequiredService<ActivityLogEventHandler>();

bus.Subscribe<TodoCreated>(statisticsHandler);
bus.Subscribe<TodoCreated>(activityLogHandler);
bus.Subscribe<TodoUpdated>(statisticsHandler);
bus.Subscribe<TodoUpdated>(activityLogHandler);
bus.Subscribe<TodoDeleted>(statisticsHandler);
bus.Subscribe<TodoDeleted>(activityLogHandler);

app.MapGet(
    "/todos",
    (TodoService service) =>
    {
        return Results.Ok(service.GetAll());
    }
);

app.MapGet(
    "/todos/{id:int}",
    (int id, TodoService service) =>
    {
        var todo = service.GetById(id);
        return todo is not null
            ? Results.Ok(todo)
            : Results.NotFound();
    }
);

app.MapPost(
    "/todos",
    async (Todo todo, TodoService service) =>
    {
        try
        {
            var created = await service.Create(todo.Title);
            return Results.Created($"/todos/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ex.Message);
        }
    }
);

app.MapPut(
    "/todos/{id:int}/complete",
    async (int id, TodoService service) =>
    {
        var success = await service.Complete(id);
        return success
            ? Results.Ok($"Todo {id} markerades som klar!")
            : Results.NotFound();
    }
);

app.MapPut(
    "/todos/{id:int}/uncomplete",
    async (int id, TodoService service) =>
    {
        var success = await service.Uncomplete(id);
        return success
            ? Results.Ok($"Todo {id} markerades som ej klar!")
            : Results.NotFound();
    }
);

app.MapDelete(
    "/todos/{id:int}",
    async (int id, TodoService service) =>
    {
        var success = await service.Delete(id);
        return success
            ? Results.NoContent()
            : Results.NotFound();
    }
);

app.MapGet(
    "/statistics",
    (IStatisticsService statistics) =>
    {
        return Results.Ok(
            new
            {
                statistics.CreatedCount,
                statistics.CompletedCount,
                statistics.DeletedCount,
            }
        );
    }
);

app.MapGet(
    "/events/stream",
    async (HttpContext context, EventMonitor monitor) =>
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
                    context.RequestAborted
                );
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // EventSource clients reconnect automatically after a disconnected request.
        }
    }
);

app.Run();
