using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;
using arkitektur.Domain.Models;
using arkitektur.Infrastructure.Simulation;

namespace arkitektur.Application.Handlers;

public sealed class TrackingProjectionEventHandler(
    ITrackingProjection tracking,
    SubscriberSimulationDelay simulation) :
    IEventHandler<ShipmentRegistered>,
    IEventHandler<ShipmentDispatched>,
    IEventHandler<ShipmentDelivered>,
    IEventHandler<ShipmentCancelled>
{
    public string ConsumerName => "Public tracking";

    public Task Handle(ShipmentRegistered @event, CancellationToken cancellationToken) =>
        Apply(@event.TrackingNumber, @event.Recipient, @event.Destination, ShipmentStatus.Registered, @event.OccurredAt, cancellationToken);

    public Task Handle(ShipmentDispatched @event, CancellationToken cancellationToken) =>
        Apply(@event.TrackingNumber, @event.Recipient, @event.Destination, ShipmentStatus.InTransit, @event.OccurredAt, cancellationToken);

    public Task Handle(ShipmentDelivered @event, CancellationToken cancellationToken) =>
        Apply(@event.TrackingNumber, @event.Recipient, @event.Destination, ShipmentStatus.Delivered, @event.OccurredAt, cancellationToken);

    public Task Handle(ShipmentCancelled @event, CancellationToken cancellationToken) =>
        Apply(@event.TrackingNumber, @event.Recipient, @event.Destination, ShipmentStatus.Cancelled, @event.OccurredAt, cancellationToken);

    private async Task Apply(
        string trackingNumber,
        string recipient,
        string destination,
        ShipmentStatus status,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        await simulation.ForPublicTracking(cancellationToken);
        tracking.Apply(trackingNumber, recipient, destination, status, occurredAt);
    }
}
