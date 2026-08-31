using arkitektur.Interfaces;

namespace arkitektur.Models.Events;

public record TodoCreated(Todo Todo) : IEvent;
