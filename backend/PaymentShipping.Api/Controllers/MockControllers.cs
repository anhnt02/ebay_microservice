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

    [HttpPost]
    public IActionResult CreateProduct([FromBody] object payload) => Ok(ApiResponse<object>.Ok(new { Id = 1 }, "", "Success"));

    [HttpPut("{id}")]
    public IActionResult UpdateProduct(int id, [FromBody] object payload) => Ok(ApiResponse<object>.Ok(new { Id = id }, "", "Success"));

    [HttpPut("{id}/inventory")]
    public IActionResult UpdateInventory(int id, [FromBody] object payload) => Ok(ApiResponse<object>.Ok(new { Id = id }, "", "Success"));

    [HttpPatch("{id}/status")]
    public IActionResult UpdateStatus(int id, [FromBody] object payload) => Ok(ApiResponse<object>.Ok(new { Id = id }, "", "Success"));

    [HttpPost("{id}/images")]
    public IActionResult UploadImages(int id) => Ok(ApiResponse<object>.Ok(new { Id = id }, "", "Success"));

    [HttpDelete("{id}/images")]
    public IActionResult DeleteImage(int id) => Ok(ApiResponse<object>.Ok(new { Id = id }, "", "Success"));

    [HttpDelete("{id}")]
    public IActionResult DeleteProduct(int id) => Ok(ApiResponse<object>.Ok(new { Id = id }, "", "Success"));
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
