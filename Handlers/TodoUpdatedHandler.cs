using arkitektur.Interfaces;
using arkitektur.Models.Events;

namespace arkitektur.Handlers;

public class TodoUpdatedHandler(IActivityLogger logger) : IEventHandler<TodoUpdated>
{
    public async Task Handle(TodoUpdated @event)
    {
        await logger.LogAsync("UPDATE", $"Todo {@event.Todo.Id} uppdaterad: [{@event.Todo.Title}] status [{@event.Todo.IsCompleted}]");
    }
}