using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;

namespace arkitektur.Application.Handlers;

public class TodoCreatedHandler(
    IActivityLogger logger,
    IStatisticsService statistics) : IEventHandler<TodoCreated>
{
    public async Task Handle(TodoCreated @event)
    {
        Console.WriteLine($"[HANDLER] TodoCreatedHandler started for Todo {@event.Todo.Id}");

        // Deliberately slow handler so the queue's asynchronous behavior is easy to see.
        await Task.Delay(TimeSpan.FromSeconds(3));

        statistics.RecordCreated();
        await logger.LogAsync(
            "TODO_CREATED",
            $"Todo skapad | ID: {@event.Todo.Id} | Titel: \"{@event.Todo.Title}\" | Status: Ej klar"
        );

        Console.WriteLine($"[HANDLER] TodoCreatedHandler finished for Todo {@event.Todo.Id}");
    }
}
