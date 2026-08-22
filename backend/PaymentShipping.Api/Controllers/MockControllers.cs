using Microsoft.AspNetCore.Mvc;
using PaymentShipping.Domain.Entities;
using PaymentShipping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PaymentShipping.Contracts;

namespace PaymentShipping.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : BaseController
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _db.Products.ToListAsync();
        var items = products.Select(p => new
        {
            p.Id, p.Title, p.Price, p.Description, p.Category, p.SellerId,
            Images = new[] { "https://picsum.photos/400" }, Seller = new { Id = p.SellerId, Username = "Seller" },
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
            p.Id, p.Title, p.Price, p.Description, p.Category, p.SellerId,
            Images = new[] { "https://picsum.photos/400" }, Seller = new { Id = p.SellerId, Username = "Seller" },
            Status = string.IsNullOrWhiteSpace(p.Status) ? "active" : p.Status,
            StockQuantity = p.StockQuantity > 0 ? p.StockQuantity : 50,
            AvailableQuantity = p.StockQuantity > 0 ? p.StockQuantity : 50,
            Quantity = p.StockQuantity > 0 ? p.StockQuantity : 50,
            InStock = true,
            IsAvailable = true
        }, "", "Success"));
    }

    public record CreateUpdateProductDto(string? Title, decimal? Price, string? Description, string? Category, int? Quantity, int? StockQuantity);
    public record UpdateInventoryDto(int Quantity);
    public record UpdateStatusDto(string? Status);

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateUpdateProductDto payload)
    {
        var qty = payload.Quantity ?? payload.StockQuantity ?? 50;
        var p = new Product { SellerId = CurrentUserId, Title = payload.Title, Price = payload.Price, Description = payload.Description, Category = payload.Category, StockQuantity = qty, Status = "active" };
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
public class SellerController : BaseController
{
    private readonly AppDbContext _db;
    public SellerController(AppDbContext db) => _db = db;

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
    public async Task<IActionResult> GetSellerOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Buyer)
            .Include(o => o.Address)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Payments)
            .Include(o => o.ShippingInfo).ThenInclude(s => s!.TrackingEvents)
            .Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == CurrentUserId))
            .OrderByDescending(o => o.OrderDate);

        var total = await query.CountAsync();
        var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        // We can just map them anonymously matching the frontend shape, or use the exact OrderDto shape.
        // Frontend expects: { id, status, orderDate, buyerName, items: [ { id, productId, sellerId, productTitle, quantity, unitPrice, lineTotal } ], shippingFee }
        var items = orders.Select(o => new
        {
            id = o.Id,
            status = o.Status,
            orderDate = o.OrderDate,
            buyerName = o.Buyer?.FullName ?? o.Buyer?.Username ?? "Guest",
            shippingFee = o.ShippingFee,
            items = o.OrderItems.Select(oi => new 
            {
                id = oi.Id,
                productId = oi.ProductId,
                sellerId = oi.Product?.SellerId ?? 2,
                productTitle = oi.Product?.Title,
                quantity = oi.Quantity,
                unitPrice = oi.UnitPrice,
                lineTotal = (oi.Product?.Price ?? 0m) * oi.Quantity
            }).ToList()
        });

        return Ok(ApiResponse<object>.Ok(new { items, page, pageSize, total }, "", "Success"));
    }

    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrderForSeller(int orderId)
    {
        var o = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Buyer)
            .Include(o => o.Address)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Payments)
            .Include(o => o.ShippingInfo).ThenInclude(s => s!.TrackingEvents)
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (o == null) return NotFound();

        var result = new
        {
            id = o.Id,
            status = o.Status,
            orderDate = o.OrderDate,
            buyerName = o.Buyer?.FullName ?? o.Buyer?.Username ?? "Guest",
            shippingFee = o.ShippingFee,
            items = o.OrderItems.Select(oi => new 
            {
                id = oi.Id,
                productId = oi.ProductId,
                sellerId = oi.Product?.SellerId ?? 2,
                productTitle = oi.Product?.Title,
                quantity = oi.Quantity,
                unitPrice = oi.UnitPrice,
                lineTotal = (oi.Product?.Price ?? 0m) * oi.Quantity
            }).ToList(),
            shippingInfo = o.ShippingInfo
        };
        return Ok(ApiResponse<object>.Ok(result, "", "Success"));
    }

    [HttpPost("orders/{orderId}/processing")]
    public async Task<IActionResult> MarkProcessing(int orderId)
    {
        var o = await _db.Orders.FindAsync(orderId);
        if (o == null) return NotFound();
        o.Status = "processing";
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { Id = orderId, Status = "processing" }, "", "Order marked as processing"));
    }

    [HttpPost("orders/{orderId}/ship")]
    public async Task<IActionResult> ShipOrder(int orderId, [FromBody] PaymentShipping.Contracts.Shipping.CreateShipmentRequest payload, [FromServices] PaymentShipping.Application.Shipping.IShippingService shipping)
    {
        var result = await shipping.CreateShipmentAsync(orderId, payload);
        return Ok(ApiResponse<object>.Ok(result, "", "Order marked as shipped"));
    }

    [HttpPost("orders/{orderId}/mock-status")]
    public async Task<IActionResult> MockStatus(int orderId, [FromBody] PaymentShipping.Contracts.Shipping.UpdateShipmentStatusRequest payload, [FromServices] PaymentShipping.Application.Shipping.IShippingService shipping)
    {
        var s = await _db.ShippingInfos.FirstOrDefaultAsync(x => x.OrderId == orderId);
        if (s == null) return NotFound("Shipping info not found for this order");
        
        var req = payload with { ShippingInfoId = s.Id };
        var result = await shipping.UpdateStatusAsync(req);
        return Ok(ApiResponse<object>.Ok(result, "", "Mock shipment updated"));
    }
}
