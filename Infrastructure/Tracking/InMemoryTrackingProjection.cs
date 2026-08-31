using arkitektur.Application.Interfaces;
using arkitektur.Domain.Models;

namespace arkitektur.Infrastructure.Tracking;

public sealed class InMemoryTrackingProjection : ITrackingProjection
{
    private readonly Dictionary<string, TrackingSnapshot> snapshots = [];
    private readonly Lock syncRoot = new();

    public TrackingSnapshot? Get(string trackingNumber)
    {
        lock (syncRoot)
        {
            return snapshots.GetValueOrDefault(trackingNumber);
        }
    }

    public void Apply(
        string trackingNumber,
        string recipient,
        string destination,
        ShipmentStatus status,
        DateTimeOffset updatedAt)
    {
        lock (syncRoot)
        {
            snapshots[trackingNumber] = new TrackingSnapshot(
                trackingNumber,
                recipient,
                destination,
                status,
                updatedAt);
        }
    }
}
