using arkitektur.Handlers;
using arkitektur.Models;
using arkitektur.Models.Events;
using arkitektur.Repositories;
using arkitektur.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<EventBus>();
builder.Services.AddSingleton<TodoRepository>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
var bus = app.Services.GetRequiredService<EventBus>();
bus.Subscribe<TodoCreated>(new TodoCreatedHandler());

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
