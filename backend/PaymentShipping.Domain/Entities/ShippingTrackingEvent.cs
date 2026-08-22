namespace PaymentShipping.Domain.Entities;

public class ShippingTrackingEvent
{
    public int Id { get; set; }
    public int ShippingInfoId { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string Provider { get; set; } = "SIMSHIP";
    public string? RawPayload { get; set; }
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ShippingInfo? ShippingInfo { get; set; }
}
