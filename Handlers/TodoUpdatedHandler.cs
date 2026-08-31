using arkitektur.Interfaces;
using arkitektur.Models.Events;

namespace arkitektur.Handlers;

public class TodoUpdatedHandler(IActivityLogger logger) : IEventHandler<TodoUpdated>
{
    public async Task Handle(TodoUpdated @event)
    {
        await logger.LogAsync("UPPDATERAD", $"Todo uppdaterad: {@event.Todo.Id} - {@event.Todo.Title} [{@event.Todo.IsCompleted}]");
    }
}