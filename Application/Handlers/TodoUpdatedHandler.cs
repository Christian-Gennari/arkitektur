using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;

namespace arkitektur.Application.Handlers;

public class TodoUpdatedHandler(IActivityLogger logger, IStatisticsService statistics)
    : IEventHandler<TodoUpdated>
{
    public async Task Handle(TodoUpdated @event)
    {
        if (@event.Todo.IsCompleted)
        {
            statistics.RecordCompleted();
        }

        await logger.LogAsync(
            "UPDATE",
            $"Todo {@event.Todo.Id} uppdaterad: [{@event.Todo.Title}] status [{@event.Todo.IsCompleted}]"
        );
    }
}
