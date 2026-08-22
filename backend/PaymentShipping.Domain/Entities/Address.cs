namespace PaymentShipping.Domain.Entities;

public class Address
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; } = "VN";
    public string? Phone { get; set; }
    public bool IsDefault { get; set; } = false;

    // Navigation
    public User? User { get; set; }
}
