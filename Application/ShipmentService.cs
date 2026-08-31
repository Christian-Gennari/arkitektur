namespace arkitektur.Application;

using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;
using arkitektur.Domain.Models;

public sealed class ShipmentService(
    IShipmentRepository repository,
    IEventPublisher eventPublisher)
{
    private readonly Lock registerLock = new();
    private readonly Lock transitionLock = new();

    public async Task<Shipment> Register(string recipient, string destination)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new ArgumentException("Recipient is required.", nameof(recipient));
        }
        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new ArgumentException("Destination is required.", nameof(destination));
        }

        Shipment shipment;
        lock (registerLock)
        {
            var id = repository.GetAll().Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
            shipment = new Shipment
            {
                Id = id,
                TrackingNumber = $"NP{DateTime.UtcNow:yyMMdd}{id:0000}",
                Recipient = recipient.Trim(),
                Destination = destination.Trim(),
                Status = ShipmentStatus.Registered,
                RegisteredAt = DateTimeOffset.UtcNow,
            };
            repository.Add(shipment);
        }

        await eventPublisher.Publish(new ShipmentRegistered(
            shipment.Id,
            shipment.TrackingNumber,
            shipment.Recipient,
            shipment.Destination));

        return shipment;
    }

    public Shipment? GetById(int id) => repository.GetById(id);

    public List<Shipment> GetAll() => repository.GetAll();

    public async Task<ShipmentTransitionResult> Dispatch(int id)
    {
        Shipment shipment;
        lock (transitionLock)
        {
            var found = repository.GetById(id);
            if (found is null) return ShipmentTransitionResult.NotFound;
            shipment = found;
            if (shipment.Status != ShipmentStatus.Registered) return ShipmentTransitionResult.InvalidState;
            shipment.Status = ShipmentStatus.InTransit;
            repository.Update(shipment);
        }
        await eventPublisher.Publish(new ShipmentDispatched(
            shipment.Id,
            shipment.TrackingNumber,
            shipment.Recipient,
            shipment.Destination));
        return ShipmentTransitionResult.Success;
    }

    public async Task<ShipmentTransitionResult> Deliver(int id)
    {
        Shipment shipment;
        lock (transitionLock)
        {
            var found = repository.GetById(id);
            if (found is null) return ShipmentTransitionResult.NotFound;
            shipment = found;
            if (shipment.Status != ShipmentStatus.InTransit) return ShipmentTransitionResult.InvalidState;
            shipment.Status = ShipmentStatus.Delivered;
            repository.Update(shipment);
        }
        await eventPublisher.Publish(new ShipmentDelivered(
            shipment.Id,
            shipment.TrackingNumber,
            shipment.Recipient,
            shipment.Destination));
        return ShipmentTransitionResult.Success;
    }

    public async Task<ShipmentTransitionResult> Cancel(int id)
    {
        Shipment shipment;
        lock (transitionLock)
        {
            var found = repository.GetById(id);
            if (found is null) return ShipmentTransitionResult.NotFound;
            shipment = found;
            if (shipment.Status is ShipmentStatus.Delivered or ShipmentStatus.Cancelled)
            {
                return ShipmentTransitionResult.InvalidState;
            }
            shipment.Status = ShipmentStatus.Cancelled;
            repository.Update(shipment);
        }
        await eventPublisher.Publish(new ShipmentCancelled(
            shipment.Id,
            shipment.TrackingNumber,
            shipment.Recipient,
            shipment.Destination));
        return ShipmentTransitionResult.Success;
    }
}

public enum ShipmentTransitionResult
{
    Success,
    NotFound,
    InvalidState,
}
