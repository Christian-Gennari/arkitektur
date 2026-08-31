namespace arkitektur.Application.Events;

public sealed record ShipmentCancelled(
    int ShipmentId,
    string TrackingNumber,
    string Recipient,
    string Destination) : DomainEvent;
