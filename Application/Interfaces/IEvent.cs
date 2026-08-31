namespace arkitektur.Application.Interfaces;

public interface IEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
    string CorrelationId { get; }
}
