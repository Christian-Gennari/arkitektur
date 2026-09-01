using System.Collections.Concurrent;
using System.Threading.Channels;
using arkitektur.Application.Interfaces;

namespace arkitektur.Infrastructure.Events;

public enum QueueEventState
{
    Queued,
    Processing,
    Completed,
    Failed
}

public sealed record QueueEventSnapshot(
    Guid Id,
    string EventType,
    QueueEventState State,
    DateTimeOffset QueuedAt,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? CompletedAt = null
);

public sealed record QueuedEvent(Guid Id, IEvent Event, Func<Task> Dispatch);

public class EventQueue
{
    private readonly Channel<QueuedEvent> queue = Channel.CreateUnbounded<QueuedEvent>();
    private readonly ConcurrentDictionary<Guid, QueueEventSnapshot> activity = new();

    public async ValueTask EnqueueAsync(
        QueuedEvent queuedEvent,
        CancellationToken cancellationToken = default)
    {
        activity[queuedEvent.Id] = new QueueEventSnapshot(
            queuedEvent.Id,
            queuedEvent.Event.GetType().Name,
            QueueEventState.Queued,
            DateTimeOffset.UtcNow
        );

        await queue.Writer.WriteAsync(queuedEvent, cancellationToken);
    }

    public IAsyncEnumerable<QueuedEvent> ReadAllAsync(CancellationToken cancellationToken)
    {
        return queue.Reader.ReadAllAsync(cancellationToken);
    }

    public void MarkProcessing(Guid id)
    {
        if (activity.TryGetValue(id, out var current))
        {
            activity[id] = current with
            {
                State = QueueEventState.Processing,
                StartedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public void MarkCompleted(Guid id)
    {
        if (activity.TryGetValue(id, out var current))
        {
            activity[id] = current with
            {
                State = QueueEventState.Completed,
                CompletedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public void MarkFailed(Guid id)
    {
        if (activity.TryGetValue(id, out var current))
        {
            activity[id] = current with
            {
                State = QueueEventState.Failed,
                CompletedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public IReadOnlyList<QueueEventSnapshot> GetRecentActivity(int count = 8)
    {
        return activity.Values
            .OrderByDescending(item => item.QueuedAt)
            .Take(count)
            .ToList();
    }
}
