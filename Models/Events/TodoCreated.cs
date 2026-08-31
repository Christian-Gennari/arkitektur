namespace arkitektur.Models.Events;
using arkitektur.Interfaces;

public record TodoCreated(Todo Todo) : IEvent;
