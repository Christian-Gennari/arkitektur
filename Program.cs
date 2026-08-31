using arkitektur.Handlers;
using arkitektur.Interfaces;
using arkitektur.Models;
using arkitektur.Models.Events;
using arkitektur.Repositories;
using arkitektur.Service;
using arkitektur.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<EventBus>();
builder.Services.AddSingleton<TodoRepository>();
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

app.Run();
