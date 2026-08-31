using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using arkitektur.Application.Interfaces;

namespace arkitektur.Infrastructure.Events;

public sealed class EventMonitor
{
    private const int HistoryLimit = 100;
    private readonly ConcurrentDictionary<Guid, Channel<EventTrace>> subscribers = new();
    private readonly Queue<EventTrace> history = new();
    private readonly Lock historyLock = new();
    private long sequence;

    public void Record(
        IEvent @event,
        string stage,
        string? consumer = null,
        string? detail = null)
    {
        var trace = new EventTrace(
            Interlocked.Increment(ref sequence),
            @event.EventId,
            @event.CorrelationId,
            @event.GetType().Name,
            stage,
            consumer,
            detail,
            DateTimeOffset.UtcNow
        );

        lock (historyLock)
        {
            history.Enqueue(trace);
            while (history.Count > HistoryLimit)
            {
                history.Dequeue();
            }
        }

        foreach (var subscriber in subscribers.Values)
        {
            subscriber.Writer.TryWrite(trace);
        }
    }

    public async IAsyncEnumerable<EventTrace> Subscribe(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<EventTrace>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
            }
        );
        subscribers[subscriberId] = channel;

        EventTrace[] snapshot;
        lock (historyLock)
        {
            snapshot = history.ToArray();
        }

        var lastSequence = 0L;

        try
        {
            foreach (var trace in snapshot)
            {
                lastSequence = trace.Sequence;
                yield return trace;
            }

            await foreach (var trace in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (trace.Sequence <= lastSequence)
                {
                    continue;
                }

                lastSequence = trace.Sequence;
                yield return trace;
            }
        }
        finally
        {
            if (subscribers.TryRemove(subscriberId, out var removed))
            {
                removed.Writer.TryComplete();
            }
        }
    }
}
