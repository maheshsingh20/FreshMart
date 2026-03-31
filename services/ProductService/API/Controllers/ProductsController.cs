using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Domain;
using ProductService.Infrastructure.Persistence;

namespace ProductService.API.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController(IProductRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? query, [FromQuery] Guid? categoryId,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice,
        [FromQuery] string? sortBy, [FromQuery] string? brand,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchAsync(query, categoryId, minPrice, maxPrice, sortBy, page, pageSize, ct, brand);
        return Ok(new { items = items.Select(ToDto), total, page, pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(id, ct);
        return p is null ? NotFound() : Ok(ToDto(p));
    }

    [HttpGet("on-sale")]
    public async Task<IActionResult> GetOnSale(CancellationToken ct)
    {
        var (items, _) = await repo.SearchAsync(null, null, null, null, null, 1, 1000, ct);
        var onSale = items.Where(p => p.DiscountPercent > 0 && p.IsActive).ToList();
        return Ok(onSale.Select(ToDto));
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> GetLowStock(CancellationToken ct)
    {
        var (items, _) = await repo.SearchAsync(null, null, null, null, null, 1, 1000, ct);
        return Ok(items.Where(p => p.StockQuantity <= 10).Select(ToDto));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest req, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(id, ct);
        if (p is null) return NotFound();
        p.Update(req.Name, req.Description, req.Price, req.ImageUrl,
            req.CategoryId, req.Brand, req.Unit, req.Weight, req.IsActive);
        p.SetDiscount(req.DiscountPercent);
        await repo.UpdateAsync(p, ct);
        return Ok(ToDto(p));
    }

    [HttpPatch("{id:guid}/discount")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> UpdateDiscount(Guid id, [FromBody] UpdateDiscountRequest req, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(id, ct);
        if (p is null) return NotFound();
        p.SetDiscount(req.DiscountPercent);
        await repo.UpdateAsync(p, ct);
        return NoContent();
    }
    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands([FromQuery] Guid? categoryId, CancellationToken ct)
    {
        var (items, _) = await repo.SearchAsync(null, categoryId, null, null, null, 1, 1000, ct);
        var brands = items.Where(p => !string.IsNullOrWhiteSpace(p.Brand))
            .Select(p => p.Brand!).Distinct().OrderBy(b => b).ToList();
        return Ok(brands);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string? q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<object>());
        var (items, _) = await repo.SearchAsync(q, null, null, null, null, 1, 8, ct);
        return Ok(items.Select(p => new { p.Id, p.Name, p.Price, p.ImageUrl, Reason = "Match" }));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> CreateProduct(CreateProductRequest req, CancellationToken ct)
    {
        var product = Product.Create(req.Name, req.Description, req.Price, req.Sku,
            req.ImageUrl, req.CategoryId, req.StockQuantity, req.Brand, req.Weight, req.Unit);
        await repo.AddAsync(product, ct);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, ToDto(product));
    }

    [HttpPatch("{id:guid}/stock")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> UpdateStock(Guid id, [FromBody] UpdateStockRequest req, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(id, ct);
        if (p is null) return NotFound();
        p.UpdateStock(req.Quantity);
        await repo.UpdateAsync(p, ct);
        return NoContent();
    }

    // Called by OrderService when an order is placed — deducts stock atomically
    [HttpPatch("{id:guid}/deduct-stock")]
    public async Task<IActionResult> DeductStock(Guid id, [FromBody] DeductStockRequest req, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(id, ct);
        if (p is null) return NotFound();

        if (p.StockQuantity < req.Quantity)
            return BadRequest(new { error = $"Insufficient stock. Available: {p.StockQuantity}, Requested: {req.Quantity}" });

        p.DeductStock(req.Quantity);
        await repo.UpdateAsync(p, ct);
        return NoContent();
    }

    private static object ToDto(Product p) => new
    {
        p.Id, p.Name, p.Description, p.Price, sku = p.SKU, p.ImageUrl,
        categoryId = p.CategoryId, categoryName = p.Category?.Name,
        p.StockQuantity, p.IsActive, p.AverageRating, p.Brand, p.Unit,
        discountPercent = p.DiscountPercent,
        discountedPrice = p.DiscountPercent > 0
            ? Math.Round(p.Price * (1 - p.DiscountPercent / 100), 2)
            : p.Price
    };
}

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController(IProductRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var cats = await repo.GetCategoriesAsync(ct);
        return Ok(cats.Select(c => new { c.Id, c.Name, c.Description, c.ImageUrl }));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory(CreateCategoryRequest req, CancellationToken ct)
    {
        var cat = Category.Create(req.Name, req.Description, req.ImageUrl);
        await repo.AddCategoryAsync(cat, ct);
        return CreatedAtAction(nameof(GetCategories), new { id = cat.Id }, new { cat.Id, cat.Name });
    }
}

public record CreateProductRequest(string Name, string Description, decimal Price, string Sku,
    string ImageUrl, Guid CategoryId, int StockQuantity, string? Brand, decimal? Weight, string? Unit);
public record UpdateProductRequest(string Name, string Description, decimal Price, string ImageUrl,
    Guid CategoryId, string? Brand, string? Unit, decimal? Weight, decimal DiscountPercent, bool IsActive);
public record UpdateStockRequest(int Quantity);
public record DeductStockRequest(int Quantity);
public record UpdateDiscountRequest(decimal DiscountPercent);
public record CreateCategoryRequest(string Name, string? Description, string? ImageUrl);
