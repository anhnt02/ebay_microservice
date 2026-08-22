using Microsoft.AspNetCore.Mvc;
using PaymentShipping.Domain.Entities;
using PaymentShipping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PaymentShipping.Contracts;

namespace PaymentShipping.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _db.Products.ToListAsync();
        var items = products.Select(p => new
        {
            p.Id, p.Title, p.Price, p.Description, p.Category,
            Images = new[] { "https://picsum.photos/400" }, Seller = new { Username = "system" },
            Status = string.IsNullOrWhiteSpace(p.Status) ? "active" : p.Status,
            StockQuantity = p.StockQuantity > 0 ? p.StockQuantity : 50,
            AvailableQuantity = p.StockQuantity > 0 ? p.StockQuantity : 50,
            Quantity = p.StockQuantity > 0 ? p.StockQuantity : 50,
            InStock = true,
            IsAvailable = true
        });
        return Ok(ApiResponse<object>.Ok(new { items, total = products.Count }, "", "Success"));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyProducts() => await GetProducts();

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();
        return Ok(ApiResponse<object>.Ok(new
        {
            p.Id, p.Title, p.Price, p.Description, p.Category,
            Images = new[] { "https://picsum.photos/400" }, Seller = new { Username = "system" },
            Status = string.IsNullOrWhiteSpace(p.Status) ? "active" : p.Status,
            StockQuantity = p.StockQuantity > 0 ? p.StockQuantity : 50,
            AvailableQuantity = p.StockQuantity > 0 ? p.StockQuantity : 50,
            Quantity = p.StockQuantity > 0 ? p.StockQuantity : 50,
            InStock = true,
            IsAvailable = true
        }, "", "Success"));
    }

    public record CreateUpdateProductDto(string Title, decimal Price, string Description, string Category, int? Quantity, int? StockQuantity);
    public record UpdateInventoryDto(int Quantity);
    public record UpdateStatusDto(string Status);

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateUpdateProductDto payload)
    {
        var qty = payload.Quantity ?? payload.StockQuantity ?? 50;
        var p = new Product { Title = payload.Title, Price = payload.Price, Description = payload.Description, Category = payload.Category, StockQuantity = qty, Status = "active" };
        _db.Products.Add(p);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { Id = p.Id, AvailableQuantity = qty, StockQuantity = qty }, "", "Success"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] CreateUpdateProductDto payload)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null) {
            p.Title = payload.Title; p.Price = payload.Price; p.Description = payload.Description; p.Category = payload.Category;
            if (payload.Quantity.HasValue || payload.StockQuantity.HasValue)
                p.StockQuantity = payload.Quantity ?? payload.StockQuantity ?? p.StockQuantity;
            await _db.SaveChangesAsync();
        }
        return Ok(ApiResponse<object>.Ok(new { Id = id }, "", "Success"));
    }

    [HttpPut("{id}/inventory")]
    public async Task<IActionResult> UpdateInventory(int id, [FromBody] UpdateInventoryDto payload)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null) {
            p.StockQuantity = payload.Quantity;
            await _db.SaveChangesAsync();
        }
        return Ok(ApiResponse<object>.Ok(new { Id = id, Quantity = payload.Quantity }, "", "Success"));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto payload)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null) {
            p.Status = payload.Status;
            await _db.SaveChangesAsync();
        }
        return Ok(ApiResponse<object>.Ok(new { Id = id }, "", "Success"));
    }

    [HttpPost("{id}/images")]
    public IActionResult UploadImages(int id) => Ok(ApiResponse<object>.Ok(new { Id = id }, "", "Success"));

    [HttpDelete("{id}/images")]
    public IActionResult DeleteImage(int id) => Ok(ApiResponse<object>.Ok(new { Id = id }, "", "Success"));

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null) {
            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
        }
        return Ok(ApiResponse<object>.Ok(new { Id = id }, "", "Success"));
    }

    [HttpPost("{id}/bids")]
    public IActionResult PlaceBid(int id, [FromBody] object payload) => Ok(ApiResponse<object>.Ok(new { Id = id, Message = "Bid placed" }, "", "Success"));

    [HttpGet("{id}/bids")]
    public IActionResult GetBids(int id) => Ok(ApiResponse<object>.Ok(new { items = new object[0], total = 0 }, "", "Success"));

    [HttpPost("{id}/auction/close")]
    public IActionResult CloseAuction(int id) => Ok(ApiResponse<object>.Ok(new { Id = id, Closed = true }, "", "Success"));

    [HttpGet("{id}/auction/winner")]
    public IActionResult GetAuctionWinner(int id) => Ok(ApiResponse<object>.Ok(new { Winner = "system", WinningBid = 100 }, "", "Success"));
}

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetCategories()
    {
        var categories = new[]
        {
            new { Id = 1, Name = "Electronics" },
            new { Id = 2, Name = "Audio" },
            new { Id = 3, Name = "Fashion" }
        };
        return Ok(ApiResponse<object>.Ok(categories, "", "Success"));
    }
}

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetMyStore()
    {
        return Ok(ApiResponse<object>.Ok(new { Id = 1, StoreName = "My Official Store", Description = "Welcome to my store", BannerImageURL = "" }, "", "Success"));
    }

    [HttpPost]
    public IActionResult CreateStore([FromBody] object payload)
    {
        return Ok(ApiResponse<object>.Ok(new { Id = 1, StoreName = "My Official Store", Description = "Created successfully" }, "", "Success"));
    }

    [HttpPut("me")]
    public IActionResult UpdateStore([FromBody] object payload)
    {
        return Ok(ApiResponse<object>.Ok(new { Id = 1, StoreName = "My Official Store", Description = "Updated successfully" }, "", "Success"));
    }
}

[ApiController]
[Route("api/[controller]")]
public class AddressesController : ControllerBase
{
    private readonly AppDbContext _db;
    public AddressesController(AppDbContext db) => _db = db;

    [HttpGet("my")]
    public async Task<IActionResult> GetMyAddresses()
    {
        var addresses = await _db.Addresses.ToListAsync();
        if (!addresses.Any())
        {
            // Seed a default address if empty
            var addr = new Address { FullName = "Anh Nguyen", Phone = "0987654321", Street = "123 Le Loi", City = "Hanoi", Province = "Hanoi", Country = "VN", PostalCode = "100000", UserId = 1 };
            _db.Addresses.Add(addr);
            await _db.SaveChangesAsync();
            addresses = new List<Address> { addr };
        }
        return Ok(ApiResponse<object>.Ok(addresses, "", "Success"));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAddress([FromBody] Address addr)
    {
        addr.UserId = 1;
        _db.Addresses.Add(addr);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(addr, "", "Address created"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAddress(int id, [FromBody] Address addr)
    {
        var existing = await _db.Addresses.FindAsync(id);
        if (existing != null)
        {
            existing.FullName = addr.FullName;
            existing.Phone = addr.Phone;
            existing.Street = addr.Street;
            existing.City = addr.City;
            existing.Province = addr.Province;
            existing.Country = addr.Country;
            existing.PostalCode = addr.PostalCode;
            await _db.SaveChangesAsync();
        }
        return Ok(ApiResponse<object>.Ok(existing ?? addr, "", "Address updated"));
    }
}

[ApiController]
[Route("api/[controller]")]
public class SellerController : ControllerBase
{
    [HttpGet("wallet")]
    public IActionResult GetWallet()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            Balance = 1550.00m,
            PendingBalance = 320.00m,
            TotalEarned = 5800.00m,
            Currency = "USD"
        }, "", "Success"));
    }

    [HttpGet("wallet/settlements")]
    public IActionResult GetSettlements([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var settlements = new[]
        {
            new { Id = 101, OrderId = 1, Amount = 999.00m, Status = "completed", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new { Id = 102, OrderId = 2, Amount = 299.00m, Status = "pending", CreatedAt = DateTime.UtcNow.AddHours(-5) }
        };
        return Ok(ApiResponse<object>.Ok(new { items = settlements, page, pageSize, total = 2 }, "", "Success"));
    }

    [HttpGet("orders")]
    public IActionResult GetSellerOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var orders = new[]
        {
            new { Id = 1, Status = "processing", TotalPrice = 999.00m, OrderDate = DateTime.UtcNow.AddDays(-1), Buyer = new { Username = "buyer1" } }
        };
        return Ok(ApiResponse<object>.Ok(new { items = orders, page, pageSize, total = 1 }, "", "Success"));
    }

    [HttpGet("orders/{orderId}")]
    public IActionResult GetOrderForSeller(int orderId)
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            Id = orderId,
            Status = "processing",
            TotalPrice = 999.00m,
            OrderDate = DateTime.UtcNow.AddDays(-1),
            Buyer = new { Username = "buyer1" }
        }, "", "Success"));
    }

    [HttpPost("orders/{orderId}/processing")]
    public IActionResult MarkProcessing(int orderId) => Ok(ApiResponse<object>.Ok(new { Id = orderId, Status = "processing" }, "", "Order marked as processing"));

    [HttpPost("orders/{orderId}/ship")]
    public IActionResult ShipOrder(int orderId, [FromBody] object payload) => Ok(ApiResponse<object>.Ok(new { Id = orderId, Status = "shipped" }, "", "Order marked as shipped"));

    [HttpPost("orders/{orderId}/mock-status")]
    public IActionResult MockStatus(int orderId, [FromBody] object payload) => Ok(ApiResponse<object>.Ok(new { Id = orderId, Status = "delivered" }, "", "Mock shipment updated"));
}
