using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;
using arkitektur.Infrastructure.Simulation;

namespace arkitektur.Application.Handlers;

public sealed class OperationsMetricsEventHandler(
    IOperationsMetrics metrics,
    SubscriberSimulationDelay simulation) :
    IEventHandler<ShipmentRegistered>,
    IEventHandler<ShipmentDispatched>,
    IEventHandler<ShipmentDelivered>,
    IEventHandler<ShipmentCancelled>
{
    public string ConsumerName => "Operations metrics";

    public Task Handle(ShipmentRegistered @event, CancellationToken cancellationToken) =>
        Record(metrics.RecordRegistered, cancellationToken);

    public Task Handle(ShipmentDispatched @event, CancellationToken cancellationToken) =>
        Record(metrics.RecordDispatched, cancellationToken);

    public Task Handle(ShipmentDelivered @event, CancellationToken cancellationToken) =>
        Record(metrics.RecordDelivered, cancellationToken);

    public Task Handle(ShipmentCancelled @event, CancellationToken cancellationToken) =>
        Record(metrics.RecordCancelled, cancellationToken);

    private async Task Record(Action update, CancellationToken cancellationToken)
    {
        await simulation.ForOperationsMetrics(cancellationToken);
        update();
    }
}
