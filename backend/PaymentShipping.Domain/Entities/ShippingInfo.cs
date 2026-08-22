namespace PaymentShipping.Domain.Entities;

public class ShippingInfo
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string TrackingCode { get; set; } = string.Empty;
    public string Carrier { get; set; } = "SimShip";
    public string Status { get; set; } = "pending";          // pending | in_transit | out_for_delivery | delivered | exception
    public string? LastCheckpoint { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAt { get; set; }

    // Navigation
    public Order? Order { get; set; }
    public ICollection<ShippingTrackingEvent> TrackingEvents { get; set; } = new List<ShippingTrackingEvent>();
}
