using System.Diagnostics;
using arkitektur.Application.Interfaces;

namespace arkitektur.Application.Events;

public abstract record DomainEvent : IEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTimeOffset.UtcNow;
        CorrelationId = Activity.Current?.TraceId.ToString() ?? EventId.ToString("N");
    }

    public Guid EventId { get; }
    public DateTimeOffset OccurredAt { get; }
    public string CorrelationId { get; }
}
