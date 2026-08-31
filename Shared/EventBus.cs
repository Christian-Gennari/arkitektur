using System.Collections.Concurrent;
using arkitektur.Models;

namespace arkitektur.Shared;

public class EventBus
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
        var eventType = typeof(TEvent);
        if (!handlers.ContainsKey(eventType)) return;
        foreach (var handler in handlers[eventType].ToList())
        {
            await ((IEventHandler<TEvent>)handler).Handle(@event);
        }
    }
}
