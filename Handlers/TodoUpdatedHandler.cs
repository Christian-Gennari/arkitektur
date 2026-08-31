using arkitektur.Interfaces;
using arkitektur.Models.Events;

namespace arkitektur.Handlers;

public class TodoUpdatedHandler(
    IActivityLogger logger,
    IStatisticsService statistics) : IEventHandler<TodoUpdated>
{
    public async Task Handle(TodoUpdated @event)
    {
        if (@event.Todo.IsCompleted)
        {
            statistics.RecordCompleted();
        }

        await logger.LogAsync("UPDATE", $"Todo {@event.Todo.Id} uppdaterad: [{@event.Todo.Title}] status [{@event.Todo.IsCompleted}]");
    }
}
