namespace arkitektur.Application.Interfaces;

public interface IEventHandler
{
    string ConsumerName { get; }
}

public interface IEventHandler<T> : IEventHandler where T : IEvent
{
    Task Handle(T @event, CancellationToken cancellationToken);
}
