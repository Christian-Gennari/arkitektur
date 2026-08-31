using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;
using arkitektur.Infrastructure.Simulation;

namespace arkitektur.Application.Handlers;

public sealed class CustomerNotificationEventHandler(
    ILogger<CustomerNotificationEventHandler> logger,
    SubscriberSimulationDelay simulation) :
    IEventHandler<ShipmentRegistered>,
    IEventHandler<ShipmentDispatched>,
    IEventHandler<ShipmentDelivered>,
    IEventHandler<ShipmentCancelled>
{
    public string ConsumerName => "Customer notifications";

    public Task Handle(ShipmentRegistered @event, CancellationToken cancellationToken) =>
        Notify(@event.TrackingNumber, @event.Recipient, "Your shipment has been registered.", cancellationToken);

    public Task Handle(ShipmentDispatched @event, CancellationToken cancellationToken) =>
        Notify(@event.TrackingNumber, @event.Recipient, "Your shipment is on its way.", cancellationToken);

    public Task Handle(ShipmentDelivered @event, CancellationToken cancellationToken) =>
        Notify(@event.TrackingNumber, @event.Recipient, "Your shipment was delivered.", cancellationToken);

    public Task Handle(ShipmentCancelled @event, CancellationToken cancellationToken) =>
        Notify(@event.TrackingNumber, @event.Recipient, "Your shipment was cancelled.", cancellationToken);

    private async Task Notify(
        string trackingNumber,
        string recipient,
        string message,
        CancellationToken cancellationToken)
    {
        await simulation.ForCustomerNotifications(cancellationToken);
        logger.LogInformation(
            "Notification for {Recipient}, shipment {TrackingNumber}: {Message}",
            recipient,
            trackingNumber,
            message);
    }
}
