namespace PaymentShipping.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public int SellerId { get; set; } = 2; // Default to seller account for demo
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Category { get; set; }
    public int StockQuantity { get; set; } = 0;
    public string? Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
