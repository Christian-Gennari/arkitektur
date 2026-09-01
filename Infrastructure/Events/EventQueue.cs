using System.Threading.Channels;
using arkitektur.Application.Interfaces;

namespace arkitektur.Infrastructure.Events;

public sealed record QueuedEvent(IEvent Event, Func<Task> Dispatch);

public class EventQueue
{
    private readonly Channel<QueuedEvent> queue = Channel.CreateUnbounded<QueuedEvent>();

    public ValueTask EnqueueAsync(
        QueuedEvent queuedEvent,
        CancellationToken cancellationToken = default)
    {
        return queue.Writer.WriteAsync(queuedEvent, cancellationToken);
    }

    public IAsyncEnumerable<QueuedEvent> ReadAllAsync(CancellationToken cancellationToken)
    {
        return queue.Reader.ReadAllAsync(cancellationToken);
    }
}
