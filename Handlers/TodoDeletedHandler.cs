using arkitektur.Interfaces;
using arkitektur.Models.Events;

namespace arkitektur.Handlers;

public class TodoDeletedHandler(IActivityLogger logger, IStatisticsService statistics)
    : IEventHandler<TodoDeleted>
{
    public async Task Handle(TodoDeleted @event)
    {
        statistics.RecordDeleted();
        await logger.LogAsync("DELETE", $"Todo raderad: {@event.Todo.Id} - {@event.Todo.Title}");
    }
}
