namespace arkitektur.Application.Events;

public sealed record TodoDeleted(int TodoId, string Title, bool WasCompleted) : DomainEvent;
