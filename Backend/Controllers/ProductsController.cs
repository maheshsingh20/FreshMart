using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? query, [FromQuery] Guid? categoryId,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice,
        [FromQuery] string? sortBy, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = db.Products.Include(p => p.Category).Where(p => p.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(p =>
                p.Name.Contains(query) ||
                p.Description.Contains(query) ||
                p.Brand!.Contains(query) ||
                p.Sku.Contains(query) ||
                p.Category.Name.Contains(query));
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId);
        if (minPrice.HasValue) q = q.Where(p => p.Price >= minPrice);
        if (maxPrice.HasValue) q = q.Where(p => p.Price <= maxPrice);
        q = sortBy switch
        {
            "price_asc" => q.OrderBy(p => p.Price),
            "price_desc" => q.OrderByDescending(p => p.Price),
            "rating" => q.OrderByDescending(p => p.AverageRating),
            _ => q.OrderBy(p => p.Name)
        };
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => ToDto(p))
            .ToListAsync();
        return Ok(new PaginatedResult<ProductDto>(items, total, page, pageSize));
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> Suggestions([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2) return Ok(Array.Empty<object>());
        var results = await db.Products
            .Where(p => p.IsActive && (p.Name.Contains(q) || p.Brand!.Contains(q) || p.Category.Name.Contains(q)))
            .OrderBy(p => p.Name)
            .Take(6)
            .Select(p => new { p.Id, p.Name, p.ImageUrl, CategoryName = p.Category.Name, p.Price })
            .ToListAsync();
        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var p = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (p == null) return NotFound();
        return Ok(ToDto(p));
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> LowStock()
    {
        var items = await db.Products.Include(p => p.Category).Where(p => p.StockQuantity < 10)
            .Select(p => ToDto(p))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> Create(CreateProductRequest req)
    {
        var category = await db.Categories.FindAsync(req.CategoryId);
        if (category == null) return BadRequest(new { error = "Category not found" });
        var product = new Product { Name = req.Name, Description = req.Description, Price = req.Price, Sku = req.Sku, ImageUrl = req.ImageUrl, CategoryId = req.CategoryId, StockQuantity = req.StockQuantity, Brand = req.Brand, Unit = req.Unit };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, ToDto(product, category.Name));
    }

    [HttpPatch("{id}/stock")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> UpdateStock(Guid id, UpdateStockRequest req)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return NotFound();
        product.StockQuantity = req.Quantity;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/discount")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> UpdateDiscount(Guid id, UpdateDiscountRequest req)
    {
        if (req.DiscountPercent < 0 || req.DiscountPercent > 100)
            return BadRequest(new { error = "Discount must be between 0 and 100" });
        var product = await db.Products.FindAsync(id);
        if (product == null) return NotFound();
        product.DiscountPercent = req.DiscountPercent;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("on-sale")]
    public async Task<IActionResult> OnSale()
    {
        var items = await db.Products.Include(p => p.Category)
            .Where(p => p.IsActive && p.DiscountPercent > 0)
            .OrderByDescending(p => p.DiscountPercent)
            .Select(p => ToDto(p))
            .ToListAsync();
        return Ok(items);
    }

    private static ProductDto ToDto(Product p, string? categoryName = null)
    {
        var cat = categoryName ?? p.Category?.Name ?? "";
        var discounted = p.DiscountPercent > 0
            ? Math.Round(p.Price * (1 - p.DiscountPercent / 100m), 2)
            : p.Price;
        return new ProductDto(p.Id.ToString(), p.Name, p.Description, p.Price, p.Sku,
            p.ImageUrl, cat, p.StockQuantity, p.IsActive, p.AverageRating,
            p.Brand, p.Unit, p.DiscountPercent, discounted);
    }
}
