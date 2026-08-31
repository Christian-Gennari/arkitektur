using arkitektur.Interfaces;

namespace arkitektur.Models.Events;

public record TodoDeleted(Todo Todo) : IEvent;