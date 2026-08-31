using arkitektur.Interfaces;

namespace arkitektur.Models.Events;

public record TodoUpdated(Todo Todo) : IEvent;