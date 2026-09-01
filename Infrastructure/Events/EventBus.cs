using System.Collections.Concurrent;
using arkitektur.Application.Interfaces;

namespace arkitektur.Infrastructure.Events;

public class EventBus(EventQueue queue) : BackgroundService, IEventPublisher
{
    private readonly ConcurrentDictionary<Type, List<IEventHandler>> handlers = new();

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        handlers.GetOrAdd(eventType, _ => new List<IEventHandler>())
            .Add(handler);
    }

    public void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        if (handlers.ContainsKey(eventType))
            handlers[eventType].Remove(handler);
    }

    public async Task Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        var queuedEvent = new QueuedEvent(
            @event,
            () => Dispatch(@event)
        );

        await queue.EnqueueAsync(queuedEvent);
        Console.WriteLine($"[QUEUE] Enqueued {typeof(TEvent).Name}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var queuedEvent in queue.ReadAllAsync(stoppingToken))
        {
            Console.WriteLine($"[QUEUE] Dequeued {queuedEvent.Event.GetType().Name}");

            try
            {
                await queuedEvent.Dispatch();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[QUEUE] Failed to process {queuedEvent.Event.GetType().Name}: {ex.Message}"
                );
            }
        }
    }

    private async Task Dispatch<TEvent>(TEvent @event) where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        if (!handlers.TryGetValue(eventType, out var eventHandlers)) return;

        foreach (var handler in eventHandlers.ToList())
        {
            await ((IEventHandler<TEvent>)handler).Handle(@event);
        }
    }
}
