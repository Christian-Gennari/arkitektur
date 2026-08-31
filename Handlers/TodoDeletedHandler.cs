using arkitektur.Interfaces;
using arkitektur.Models.Events;

namespace arkitektur.Handlers;

public class TodoDeletedHandler(IActivityLogger logger) : IEventHandler<TodoDeleted>
{
    public async Task Handle(TodoDeleted @event)
    {
        await logger.LogAsync("RADERAD", $"Todo raderad: {@event.Todo.Id} - {@event.Todo.Title}");
    }
}