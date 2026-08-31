namespace arkitektur.Application.Events;

public sealed record ShipmentDispatched(
    int ShipmentId,
    string TrackingNumber,
    string Recipient,
    string Destination) : DomainEvent;
