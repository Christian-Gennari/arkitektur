using arkitektur.Interfaces;
using arkitektur.Models;
using arkitektur.Models.Events;

namespace arkitektur.Handlers;

public class TodoCreatedHandler(IActivityLogger logger) : IEventHandler<TodoCreated>
{
    public async Task Handle(TodoCreated @event)
    {
        await logger.LogAsync("CREATE", $"Todo {@event.Todo.Id} skapad: {@event.Todo.Title}");
    }
}
