using arkitektur.Application.Interfaces;
using arkitektur.Domain.Models;

namespace arkitektur.Infrastructure.Repositories;

public sealed class InMemoryShipmentRepository : IShipmentRepository
{
    private readonly List<Shipment> shipments = [];
    private readonly Lock syncRoot = new();

    public List<Shipment> GetAll()
    {
        lock (syncRoot)
        {
            return shipments.Select(Clone).ToList();
        }
    }

    public Shipment? GetById(int id)
    {
        lock (syncRoot)
        {
            var shipment = shipments.FirstOrDefault(item => item.Id == id);
            return shipment is null ? null : Clone(shipment);
        }
    }

    public void Add(Shipment shipment)
    {
        lock (syncRoot)
        {
            shipments.Add(Clone(shipment));
        }
    }

    public void Update(Shipment shipment)
    {
        lock (syncRoot)
        {
            var existing = shipments.FirstOrDefault(item => item.Id == shipment.Id);
            if (existing is not null)
            {
                existing.TrackingNumber = shipment.TrackingNumber;
                existing.Recipient = shipment.Recipient;
                existing.Destination = shipment.Destination;
                existing.Status = shipment.Status;
                existing.RegisteredAt = shipment.RegisteredAt;
            }
        }
    }

    private static Shipment Clone(Shipment shipment)
    {
        return new Shipment
        {
            Id = shipment.Id,
            TrackingNumber = shipment.TrackingNumber,
            Recipient = shipment.Recipient,
            Destination = shipment.Destination,
            Status = shipment.Status,
            RegisteredAt = shipment.RegisteredAt,
        };
    }
}
