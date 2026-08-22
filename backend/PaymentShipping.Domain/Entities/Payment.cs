namespace PaymentShipping.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string Provider { get; set; } = string.Empty;   // paypal | cod
    public string Status { get; set; } = "pending";        // pending | captured | cancelled | failed
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? TransactionId { get; set; }
    public string? ProviderRawResponse { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    // Navigation
    public Order? Order { get; set; }
    public User? User { get; set; }
}
