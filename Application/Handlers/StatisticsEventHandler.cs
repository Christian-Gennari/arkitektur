using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;

namespace arkitektur.Application.Handlers;

public sealed class StatisticsEventHandler(IStatisticsService statistics) :
    IEventHandler<TodoCreated>,
    IEventHandler<TodoUpdated>,
    IEventHandler<TodoDeleted>
{
    public string ConsumerName => "Statistics";

    public Task Handle(TodoCreated @event, CancellationToken cancellationToken)
    {
        statistics.RecordCreated();
        return Task.CompletedTask;
    }

    public Task Handle(TodoUpdated @event, CancellationToken cancellationToken)
    {
        if (@event.IsCompleted)
        {
            statistics.RecordCompleted();
        }

        return Task.CompletedTask;
    }

    public Task Handle(TodoDeleted @event, CancellationToken cancellationToken)
    {
        statistics.RecordDeleted();
        return Task.CompletedTask;
    }
}
