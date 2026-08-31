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
            @event.Todo.IsCompleted ? "TODO_COMPLETED" : "TODO_REOPENED",
            @event.Todo.IsCompleted
                ? $"Todo markerad som klar | ID: {@event.Todo.Id} | Titel: \"{@event.Todo.Title}\" | Status: Klar"
                : $"Todo öppnad igen | ID: {@event.Todo.Id} | Titel: \"{@event.Todo.Title}\" | Status: Ej klar"
        );
    }
}
