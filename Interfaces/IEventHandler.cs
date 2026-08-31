namespace arkitektur.Interfaces;

public interface IEventHandler
{
}

public interface IEventHandler<T> : IEventHandler where T : IEvent
{
    public Task Handle(T @event);
}
