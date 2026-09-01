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

builder.Services.AddSingleton<EventQueue>();
builder.Services.AddSingleton<EventBus>();
builder.Services.AddSingleton<IEventPublisher>(services =>
    services.GetRequiredService<EventBus>());
builder.Services.AddHostedService(services =>
    services.GetRequiredService<EventBus>());
builder.Services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();
builder.Services.AddSingleton<IActivityLogger, FileActivityLogger>();
builder.Services.AddSingleton<TodoCreatedHandler>();
builder.Services.AddSingleton<TodoUpdatedHandler>();
builder.Services.AddSingleton<TodoDeletedHandler>();
builder.Services.AddSingleton<TodoService>();
builder.Services.AddSingleton<IStatisticsService, StatisticsService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
var bus = app.Services.GetRequiredService<EventBus>();
var todoCreatedHandler = app.Services.GetRequiredService<TodoCreatedHandler>();
var todoUpdatedHandler = app.Services.GetRequiredService<TodoUpdatedHandler>();
var todoDeletedHandler = app.Services.GetRequiredService<TodoDeletedHandler>();
bus.Subscribe<TodoCreated>(todoCreatedHandler);
bus.Subscribe<TodoUpdated>(todoUpdatedHandler);
bus.Subscribe<TodoDeleted>(todoDeletedHandler);

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
    "/event-queue",
    (EventQueue queue) =>
    {
        return Results.Ok(
            queue.GetRecentActivity().Select(item => new
            {
                item.Id,
                item.EventType,
                Status = item.State.ToString(),
                item.QueuedAt,
                item.StartedAt,
                item.CompletedAt,
            })
        );
    }
);

app.Run();
