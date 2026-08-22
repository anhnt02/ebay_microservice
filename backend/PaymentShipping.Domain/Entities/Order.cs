namespace PaymentShipping.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public int BuyerId { get; set; }
    public int? AddressId { get; set; }
    public int? CouponId { get; set; }
    public string? CouponCode { get; set; }
    public string PaymentMethod { get; set; } = "paypal";
    public string Status { get; set; } = "pending_payment";
    public decimal SubtotalAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public User? Buyer { get; set; }
    public Address? Address { get; set; }
    public Coupon? Coupon { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ShippingInfo? ShippingInfo { get; set; }
}
