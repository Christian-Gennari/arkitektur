using arkitektur.Interfaces;
using arkitektur.Models;
using arkitektur.Models.Events;

namespace arkitektur.Handlers;

public class TodoCreatedHandler(
    IActivityLogger logger,
    IStatisticsService statistics) : IEventHandler<TodoCreated>
{
    public async Task Handle(TodoCreated @event)
    {
        statistics.RecordCreated();
        await logger.LogAsync("CREATE", $"Todo {@event.Todo.Id} skapad: {@event.Todo.Title}");
        await logger.LogAsync("SKAPAD", $"Todo skapad: {@event.Todo.Id} - {@event.Todo.Title}");
    }
}
