using arkitektur.Interfaces;
using arkitektur.Models.Events;

namespace arkitektur.Handlers;

public class TodoDeletedHandler(IActivityLogger logger) : IEventHandler<TodoDeleted>
{
    public async Task Handle(TodoDeleted @event)
    {
        await logger.LogAsync("DELETE", $"Todo {@event.Todo.Id} raderad: {@event.Todo.Title}");
    }
}