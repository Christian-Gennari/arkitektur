namespace arkitektur.Application.Interfaces;

public interface IEventPublisher
{
    Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}
