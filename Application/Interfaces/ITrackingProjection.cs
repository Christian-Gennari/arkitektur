using arkitektur.Domain.Models;

namespace arkitektur.Application.Interfaces;

public interface ITrackingProjection
{
    TrackingSnapshot? Get(string trackingNumber);
    void Apply(string trackingNumber, string recipient, string destination, ShipmentStatus status, DateTimeOffset updatedAt);
}

public sealed record TrackingSnapshot(
    string TrackingNumber,
    string Recipient,
    string Destination,
    ShipmentStatus Status,
    DateTimeOffset UpdatedAt);
