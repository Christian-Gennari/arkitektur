using arkitektur.Models;
using arkitektur.Models.Events;

namespace arkitektur.Handlers;

public class TodoCreatedHandler : IEventHandler<TodoCreated>
{
    public Task Handle(TodoCreated @event)
    {
        Console.WriteLine($"Todo skapad: {@event.Todo.Title}");
        return Task.CompletedTask;
    }
}