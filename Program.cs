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
builder.Services.AddSingleton<TodoService>();
builder.Services.AddSingleton<IStatisticsService, StatisticsService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
var bus = app.Services.GetRequiredService<EventBus>();
var todoCreatedHandler = app.Services.GetRequiredService<TodoCreatedHandler>();
bus.Subscribe<TodoCreated>(todoCreatedHandler);

app.MapPost(
    "/todos",
    async (Todo todo, TodoService service) =>
    {
        var created = await service.Create(todo.Title);
        return Results.Created($"/todos/{created.Id}", created);
    }
);

app.Run();
