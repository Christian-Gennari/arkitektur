namespace arkitektur.Application.Events;

public sealed record ShipmentRegistered(
    int ShipmentId,
    string TrackingNumber,
    string Recipient,
    string Destination) : DomainEvent;
