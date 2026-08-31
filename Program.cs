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

app.MapPost(
    "/todos",
    async (Todo todo, TodoService service) =>
    {
        var created = await service.Create(todo.Title);
        return Results.Created($"/todos/{created.Id}", created);
    }
);

app.Run();
