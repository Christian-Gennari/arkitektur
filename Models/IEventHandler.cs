namespace arkitektur.Models;

public interface IEventHandler
{
}

public interface IEventHandler<T> : IEventHandler where T : IEvent
{
    public Task Handle(T @event);
}