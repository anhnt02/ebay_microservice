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
            p.Status, p.StockQuantity
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
            p.Status, p.StockQuantity
        }, "", "Success"));
    }

    public record CreateUpdateProductDto(string Title, decimal Price, string Description, string Category);
    public record UpdateInventoryDto(int Quantity);
    public record UpdateStatusDto(string Status);

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateUpdateProductDto payload)
    {
        var p = new Product { Title = payload.Title, Price = payload.Price, Description = payload.Description, Category = payload.Category };
        _db.Products.Add(p);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { Id = p.Id }, "", "Success"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] CreateUpdateProductDto payload)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null) {
            p.Title = payload.Title; p.Price = payload.Price; p.Description = payload.Description; p.Category = payload.Category;
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
        return Ok(ApiResponse<object>.Ok(new { Id = id }, "", "Success"));
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
        return Ok(ApiResponse<object>.Ok(new { Id = 1, StoreName = "Mock Store", Description = "Mock Description" }, "", "Success"));
    }

    [HttpPost]
    public IActionResult CreateStore([FromBody] object payload)
    {
        return Ok(ApiResponse<object>.Ok(new { Id = 1, StoreName = "Mock Store", Description = "Mock Description" }, "", "Success"));
    }

    [HttpPut("me")]
    public IActionResult UpdateStore([FromBody] object payload)
    {
        return Ok(ApiResponse<object>.Ok(new { Id = 1, StoreName = "Mock Store", Description = "Mock Description" }, "", "Success"));
    }
}
