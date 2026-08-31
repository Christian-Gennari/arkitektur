using arkitektur.Handlers;
using arkitektur.Models;
using arkitektur.Models.Events;
using arkitektur.Repositories;
using arkitektur.Shared;
using arkitektur.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<EventBus>();
builder.Services.AddSingleton<TodoRepository>();
builder.Services.AddSingleton<TodoService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
var bus = app.Services.GetRequiredService<EventBus>();
bus.Subscribe<TodoCreated>(new TodoCreatedHandler());

app.MapPost(
    "/todos",
    async (Todo todo, TodoService service) =>
    {
        var created = await service.Create(todo.Title);
        return Results.Created($"/todos/{created.Id}", created);
    }
);

app.Run();
