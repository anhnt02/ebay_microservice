using Microsoft.EntityFrameworkCore;
using PaymentShipping.Domain.Entities;

namespace PaymentShipping.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ShippingInfo> ShippingInfos => Set<ShippingInfo>();
    public DbSet<ShippingTrackingEvent> ShippingTrackingEvents => Set<ShippingTrackingEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(200);
            e.HasIndex(x => x.Email).IsUnique();
        });

        // ── Address ───────────────────────────────────────────────────
        modelBuilder.Entity<Address>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.Street).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.Province).HasMaxLength(100);
            e.Property(x => x.Country).HasMaxLength(10).HasDefaultValue("VN");
            e.HasOne(x => x.User)
             .WithMany(x => x.Addresses)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Product ───────────────────────────────────────────────────
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(500);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("active");
        });

        // ── Coupon ────────────────────────────────────────────────────
        modelBuilder.Entity<Coupon>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.HasIndex(x => x.Code).IsUnique();
        });

        // ── Order ─────────────────────────────────────────────────────
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("pending_payment");
            e.Property(x => x.PaymentMethod).HasMaxLength(50).HasDefaultValue("paypal");
            e.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
            e.Property(x => x.ShippingFee).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalPrice).HasPrecision(18, 2);
            e.Property(x => x.CouponCode).HasMaxLength(100);

            e.HasOne(x => x.Buyer)
             .WithMany(x => x.Orders)
             .HasForeignKey(x => x.BuyerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Address)
             .WithMany()
             .HasForeignKey(x => x.AddressId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Coupon)
             .WithMany()
             .HasForeignKey(x => x.CouponId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.ShippingInfo)
             .WithOne(x => x.Order)
             .HasForeignKey<ShippingInfo>(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── OrderItem ─────────────────────────────────────────────────
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Ignore(x => x.LineTotal);   // computed, not stored

            e.HasOne(x => x.Order)
             .WithMany(x => x.OrderItems)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
             .WithMany(x => x.OrderItems)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Payment ───────────────────────────────────────────────────
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Provider).HasMaxLength(50);
            e.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("pending");
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(10).HasDefaultValue("USD");
            e.Property(x => x.TransactionId).HasMaxLength(200);
            e.Property(x => x.ProviderRawResponse).HasMaxLength(4000);

            e.HasOne(x => x.Order)
             .WithMany(x => x.Payments)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ShippingInfo ──────────────────────────────────────────────
        modelBuilder.Entity<ShippingInfo>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TrackingCode).HasMaxLength(100);
            e.Property(x => x.Carrier).HasMaxLength(100).HasDefaultValue("SimShip");
            e.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("pending");
            e.Property(x => x.LastCheckpoint).HasMaxLength(500);
        });

        // ── ShippingTrackingEvent ─────────────────────────────────────
        modelBuilder.Entity<ShippingTrackingEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Location).HasMaxLength(200);
            e.Property(x => x.Provider).HasMaxLength(50).HasDefaultValue("SIMSHIP");
            e.Property(x => x.RawPayload).HasMaxLength(4000);

            e.HasOne(x => x.ShippingInfo)
             .WithMany(x => x.TrackingEvents)
             .HasForeignKey(x => x.ShippingInfoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seed Data ─────────────────────────────────────────────────
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default buyer & seller
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "anhnt",
                Email = "sicano20@gmail.com",
                PasswordHash = "dummy_hash",
                FullName = "Anh Nguyen (Buyer)",
                Phone = "0987654321",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new User
            {
                Id = 2,
                Username = "seller_store",
                Email = "seller@ebay.com",
                PasswordHash = "dummy_hash",
                FullName = "Official Store (Seller)",
                Phone = "0912345678",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        );

        // Seed default address
        modelBuilder.Entity<Address>().HasData(
            new Address
            {
                Id = 1,
                UserId = 1,
                FullName = "Anh Nguyen",
                Phone = "0987654321",
                Street = "123 Le Loi",
                City = "Hanoi",
                Province = "Hanoi",
                Country = "VN",
                PostalCode = "100000"
            }
        );

        // Seed products for testing
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Title = "iPhone 15 Pro", Description = "Apple iPhone 15 Pro 256GB", Price = 999.00m, Category = "Electronics", StockQuantity = 50, Status = "active" },
            new Product { Id = 2, Title = "Samsung Galaxy S24", Description = "Samsung Galaxy S24 128GB", Price = 799.00m, Category = "Electronics", StockQuantity = 30, Status = "active" },
            new Product { Id = 3, Title = "Sony WH-1000XM5", Description = "Sony Noise Cancelling Headphones", Price = 299.00m, Category = "Audio", StockQuantity = 100, Status = "active" }
        );

        // Seed coupon
        modelBuilder.Entity<Coupon>().HasData(
            new Coupon { Id = 1, Code = "SAVE10", DiscountPercent = 10, MaxUsage = 100, UsedCount = 0, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Coupon { Id = 2, Code = "SAVE20", DiscountPercent = 20, MaxUsage = 50, UsedCount = 0, IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
