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
            p.Id,
            p.Title,
            p.Price,
            p.Description,
            p.Category,
            Images = new[] { "https://picsum.photos/400" },
            Seller = new { Username = "system" },
            p.Status,
            p.StockQuantity
        });
        
        return Ok(ApiResponse<object>.Ok(new { items, total = products.Count }, "", "Success"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();
        return Ok(ApiResponse<object>.Ok(new
        {
            p.Id,
            p.Title,
            p.Price,
            p.Description,
            p.Category,
            Images = new[] { "https://picsum.photos/400" },
            Seller = new { Username = "system" },
            p.Status,
            p.StockQuantity
        }, "", "Success"));
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
