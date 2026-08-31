using arkitektur.Application.Interfaces;
using arkitektur.Domain.Models;

namespace arkitektur.Application.Events;

public record TodoUpdated(Todo Todo) : IEvent;
