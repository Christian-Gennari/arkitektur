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

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
var bus = app.Services.GetRequiredService<EventBus>();
bus.Subscribe<TodoCreated>(app.Services.GetRequiredService<TodoCreatedHandler>());

app.MapPost(
    "/todos",
    async (Todo todo, TodoRepository repo, EventBus bus) =>
    {
        repo.Add(todo);
        await bus.Publish(new TodoCreated(todo));
        return Results.Created($"/todos/{todo.Id}", todo);
    }
);

app.Run();
