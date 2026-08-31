namespace arkitektur.Application.Events;

public sealed record TodoUpdated(int TodoId, string Title, bool IsCompleted) : DomainEvent;
