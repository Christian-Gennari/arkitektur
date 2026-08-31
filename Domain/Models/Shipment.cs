using System.Text.Json.Serialization;

namespace arkitektur.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShipmentStatus
{
    Registered,
    InTransit,
    Delivered,
    Cancelled,
}

public sealed class Shipment
{
    public int Id { get; set; }
    public string TrackingNumber { get; set; } = "";
    public string Recipient { get; set; } = "";
    public string Destination { get; set; } = "";
    public ShipmentStatus Status { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
}
