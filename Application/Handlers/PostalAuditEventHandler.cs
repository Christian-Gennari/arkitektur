using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;
using arkitektur.Infrastructure.Simulation;

namespace arkitektur.Application.Handlers;

public sealed class PostalAuditEventHandler(
    IPostalAuditLog auditLog,
    SubscriberSimulationDelay simulation) :
    IEventHandler<ShipmentRegistered>,
    IEventHandler<ShipmentDispatched>,
    IEventHandler<ShipmentDelivered>,
    IEventHandler<ShipmentCancelled>
{
    public string ConsumerName => "Postal audit";

    public Task Handle(ShipmentRegistered @event, CancellationToken cancellationToken) =>
        Write("SHIPMENT_REGISTERED", @event.TrackingNumber, @event.Recipient, @event.Destination, cancellationToken);

    public Task Handle(ShipmentDispatched @event, CancellationToken cancellationToken) =>
        Write("SHIPMENT_DISPATCHED", @event.TrackingNumber, @event.Recipient, @event.Destination, cancellationToken);

    public Task Handle(ShipmentDelivered @event, CancellationToken cancellationToken) =>
        Write("SHIPMENT_DELIVERED", @event.TrackingNumber, @event.Recipient, @event.Destination, cancellationToken);

    public Task Handle(ShipmentCancelled @event, CancellationToken cancellationToken) =>
        Write("SHIPMENT_CANCELLED", @event.TrackingNumber, @event.Recipient, @event.Destination, cancellationToken);

    private async Task Write(
        string eventName,
        string trackingNumber,
        string recipient,
        string destination,
        CancellationToken cancellationToken)
    {
        await simulation.ForPostalAudit(cancellationToken);
        await auditLog.WriteAsync(
            eventName,
            $"Tracking: {trackingNumber} | Recipient: {recipient} | Destination: {destination}");
    }
}
