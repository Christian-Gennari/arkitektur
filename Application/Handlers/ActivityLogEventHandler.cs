using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;

namespace arkitektur.Application.Handlers;

public sealed class ActivityLogEventHandler(IActivityLogger logger) :
    IEventHandler<TodoCreated>,
    IEventHandler<TodoUpdated>,
    IEventHandler<TodoDeleted>
{
    public string ConsumerName => "Activity log";

    public Task Handle(TodoCreated @event, CancellationToken cancellationToken)
    {
        return logger.LogAsync(
            "TODO_CREATED",
            $"Todo skapad | ID: {@event.TodoId} | Titel: \"{@event.Title}\" | Status: Ej klar"
        );
    }

    public Task Handle(TodoUpdated @event, CancellationToken cancellationToken)
    {
        return logger.LogAsync(
            @event.IsCompleted ? "TODO_COMPLETED" : "TODO_REOPENED",
            @event.IsCompleted
                ? $"Todo markerad som klar | ID: {@event.TodoId} | Titel: \"{@event.Title}\" | Status: Klar"
                : $"Todo öppnad igen | ID: {@event.TodoId} | Titel: \"{@event.Title}\" | Status: Ej klar"
        );
    }

    public Task Handle(TodoDeleted @event, CancellationToken cancellationToken)
    {
        return logger.LogAsync(
            "TODO_DELETED",
            $"Todo raderad | ID: {@event.TodoId} | Titel: \"{@event.Title}\" | Tidigare status: {(@event.WasCompleted ? "Klar" : "Ej klar")}"
        );
    }
}
