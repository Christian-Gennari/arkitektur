using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;

namespace arkitektur.Application.Handlers;

public class TodoDeletedHandler(IActivityLogger logger, IStatisticsService statistics)
    : IEventHandler<TodoDeleted>
{
    public async Task Handle(TodoDeleted @event)
    {
        statistics.RecordDeleted();
        await logger.LogAsync(
            "TODO_DELETED",
            $"Todo raderad | ID: {@event.Todo.Id} | Titel: \"{@event.Todo.Title}\""
        );
    }
}
