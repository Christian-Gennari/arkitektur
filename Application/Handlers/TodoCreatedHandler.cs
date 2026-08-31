using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;

namespace arkitektur.Application.Handlers;

public class TodoCreatedHandler(
    IActivityLogger logger,
    IStatisticsService statistics) : IEventHandler<TodoCreated>
{
    public async Task Handle(TodoCreated @event)
    {
        statistics.RecordCreated();
        await logger.LogAsync("CREATE", $"Todo skapad: {@event.Todo.Id} - {@event.Todo.Title}");
    }
}
