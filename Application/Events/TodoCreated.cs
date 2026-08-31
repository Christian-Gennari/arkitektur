namespace arkitektur.Application.Events;

public sealed record TodoCreated(int TodoId, string Title) : DomainEvent;
