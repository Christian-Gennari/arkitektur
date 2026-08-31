namespace arkitektur.Application.Events;

public sealed record ShipmentDelivered(
    int ShipmentId,
    string TrackingNumber,
    string Recipient,
    string Destination) : DomainEvent;
