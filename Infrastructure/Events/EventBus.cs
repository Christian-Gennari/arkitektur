using System.Collections.Concurrent;
using System.Threading.Channels;
using arkitektur.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace arkitektur.Infrastructure.Events;

public sealed class EventBus : BackgroundService, IEventPublisher
{
    private sealed record EventSubscription(
        IEventHandler Handler,
        Func<IEvent, CancellationToken, Task> Handle
    );

    private readonly ConcurrentDictionary<Type, List<EventSubscription>> handlers = new();
    private readonly Channel<IEvent> queue;
    private readonly EventMonitor monitor;
    private readonly EventProcessingOptions options;
    private readonly ILogger<EventBus> logger;
    private readonly SemaphoreSlim publishLock = new(1, 1);

    public EventBus(
        EventMonitor monitor,
        IOptions<EventProcessingOptions> options,
        ILogger<EventBus> logger)
    {
        this.monitor = monitor;
        this.options = options.Value;
        this.logger = logger;

        var capacity = Math.Max(1, this.options.QueueCapacity);
        queue = Channel.CreateBounded<IEvent>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            }
        );
    }

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        var eventHandlers = handlers.GetOrAdd(typeof(TEvent), _ => []);
        lock (eventHandlers)
        {
            if (!eventHandlers.Any(subscription => ReferenceEquals(subscription.Handler, handler)))
            {
                eventHandlers.Add(
                    new EventSubscription(
                        handler,
                        (@event, cancellationToken) =>
                            handler.Handle((TEvent)@event, cancellationToken)
                    )
                );
            }
        }
    }

    public async Task Publish<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        await publishLock.WaitAsync(cancellationToken);
        try
        {
            if (!await queue.Writer.WaitToWriteAsync(cancellationToken))
            {
                throw new InvalidOperationException("Event queue is no longer accepting events.");
            }

            monitor.Record(@event, EventStages.Queued);
            if (!queue.Writer.TryWrite(@event))
            {
                throw new InvalidOperationException("Event could not be added to the queue.");
            }
        }
        finally
        {
            publishLock.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var @event in queue.Reader.ReadAllAsync(stoppingToken))
            {
                await Process(@event, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown. This educational queue is intentionally non-durable.
        }
    }

    private async Task Process(IEvent @event, CancellationToken cancellationToken)
    {
        if (options.DemoDelayMilliseconds > 0)
        {
            await Task.Delay(options.DemoDelayMilliseconds, cancellationToken);
        }

        var eventHandlers = GetHandlers(@event.GetType());
        monitor.Record(
            @event,
            EventStages.Processing,
            detail: $"{eventHandlers.Count} consumers"
        );

        var results = await Task.WhenAll(
            eventHandlers.Select(handler => RunHandler(handler, @event, cancellationToken))
        );

        monitor.Record(
            @event,
            results.All(succeeded => succeeded)
                ? EventStages.Completed
                : EventStages.CompletedWithErrors
        );
    }

    private List<EventSubscription> GetHandlers(Type eventType)
    {
        if (!handlers.TryGetValue(eventType, out var eventHandlers))
        {
            return [];
        }

        lock (eventHandlers)
        {
            return eventHandlers.ToList();
        }
    }

    private async Task<bool> RunHandler(
        EventSubscription subscription,
        IEvent @event,
        CancellationToken cancellationToken)
    {
        var consumerName = subscription.Handler.ConsumerName;
        monitor.Record(@event, EventStages.ConsumerStarted, consumerName);

        try
        {
            await subscription.Handle(@event, cancellationToken);
            monitor.Record(@event, EventStages.ConsumerCompleted, consumerName);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Consumer {ConsumerName} failed while handling event {EventId}",
                consumerName,
                @event.EventId
            );
            monitor.Record(
                @event,
                EventStages.ConsumerFailed,
                consumerName,
                exception.Message
            );
            return false;
        }
    }
}
