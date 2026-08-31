namespace arkitektur.Models.Events;
using Shared;

public record TodoCreated(Todo Todo) : IEvent;