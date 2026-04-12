using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Domain;
using ProductService.Infrastructure.Persistence;

namespace ProductService.API.Controllers;

/// <summary>
/// HTTP API controller for product catalogue management.
/// Exposes endpoints for browsing, searching, creating, and updating products,
/// as well as stock management operations. Public read endpoints (search, get by ID,
/// on-sale, suggestions) require no authentication so anonymous users can browse
/// the catalogue. Write operations (create, update, stock management) are restricted
/// to Admin and StoreManager roles. The <c>deduct-stock</c> endpoint is intentionally
/// unauthenticated at the HTTP level because it is called service-to-service by
/// OrderService after a successful order placement.
/// </summary>
[ApiController]
[Route("api/v1/products")]
public class ProductsController(IProductRepository repo) : ControllerBase
{
    /// <summary>
    /// Searches and filters the product catalogue with support for full-text search,
    /// category filtering, price range filtering, brand filtering, and sorting.
    /// Returns a paginated result set so the frontend can implement infinite scroll
    /// or traditional pagination without loading the entire catalogue.
    /// </summary>
    /// <param name="query">Optional full-text search term matched against product name and description.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="minPrice">Optional minimum price filter (inclusive).</param>
    /// <param name="maxPrice">Optional maximum price filter (inclusive).</param>
    /// <param name="sortBy">Optional sort field (e.g. "price", "rating", "newest").</param>
    /// <param name="brand">Optional brand filter.</param>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Number of products per page (default 20).</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Retrieves a single product by its unique identifier.
    /// Returns 404 if the product does not exist or has been deleted.
    /// The response includes the computed discounted price so the frontend
    /// does not need to recalculate it.
    /// </summary>
    /// <param name="id">The product's unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(id, ct);
        return p is null ? NotFound() : Ok(ToDto(p));
    }

    /// <summary>
    /// Returns all active products that currently have a discount applied
    /// (i.e. <c>DiscountPercent > 0</c>). Used to populate the "On Sale" section
    /// of the storefront. Loads up to 1000 products and filters in memory —
    /// acceptable for a catalogue of typical size.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("on-sale")]
    public async Task<IActionResult> GetOnSale(CancellationToken ct)
    {
        var (items, _) = await repo.SearchAsync(null, null, null, null, null, 1, 1000, ct);
        var onSale = items.Where(p => p.DiscountPercent > 0 && p.IsActive).ToList();
        return Ok(onSale.Select(ToDto));
    }

    /// <summary>
    /// Returns all products with 10 or fewer units in stock.
    /// Used by the admin/store manager dashboard to identify items that need
    /// restocking before they go out of stock and block new orders.
    /// Restricted to Admin and StoreManager roles.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> GetLowStock(CancellationToken ct)
    {
        var (items, _) = await repo.SearchAsync(null, null, null, null, null, 1, 1000, ct);
        return Ok(items.Where(p => p.StockQuantity <= 10).Select(ToDto));
    }

    /// <summary>
    /// Replaces all mutable fields of an existing product.
    /// Also applies the discount percentage in the same request so admins can
    /// update product details and pricing in a single operation.
    /// Restricted to Admin and StoreManager roles.
    /// </summary>
    /// <param name="id">The product to update.</param>
    /// <param name="req">The new product data.</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Updates only the discount percentage for a product without touching other fields.
    /// Useful for running flash sales or applying bulk discounts without a full product edit.
    /// Restricted to Admin and StoreManager roles.
    /// </summary>
    /// <param name="id">The product to discount.</param>
    /// <param name="req">The new discount percentage (0 to remove the discount).</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Returns a distinct, sorted list of brand names available in the catalogue,
    /// optionally filtered by category. Used to populate the brand filter facet
    /// in the product search UI.
    /// </summary>
    /// <param name="categoryId">Optional category to scope the brand list.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands([FromQuery] Guid? categoryId, CancellationToken ct)
    {
        var (items, _) = await repo.SearchAsync(null, categoryId, null, null, null, 1, 1000, ct);
        var brands = items.Where(p => !string.IsNullOrWhiteSpace(p.Brand))
            .Select(p => p.Brand!).Distinct().OrderBy(b => b).ToList();
        return Ok(brands);
    }

    /// <summary>
    /// Returns up to 8 product suggestions matching the search query.
    /// Used to power the search autocomplete/typeahead in the storefront header.
    /// Returns an empty array for blank queries to avoid unnecessary database hits.
    /// </summary>
    /// <param name="q">The partial search term typed by the user.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string? q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<object>());
        var (items, _) = await repo.SearchAsync(q, null, null, null, null, 1, 8, ct);
        return Ok(items.Select(p => new { p.Id, p.Name, p.Price, p.ImageUrl, Reason = "Match" }));
    }

    /// <summary>
    /// Creates a new product in the catalogue.
    /// The SKU must be unique across all products; the repository enforces this constraint.
    /// Restricted to Admin and StoreManager roles.
    /// </summary>
    /// <param name="req">The product details including initial stock quantity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 Created with the new product's data and a Location header.</returns>
    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> CreateProduct(CreateProductRequest req, CancellationToken ct)
    {
        var product = Product.Create(req.Name, req.Description, req.Price, req.Sku,
            req.ImageUrl, req.CategoryId, req.StockQuantity, req.Brand, req.Weight, req.Unit);
        await repo.AddAsync(product, ct);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, ToDto(product));
    }

    /// <summary>
    /// Sets the absolute stock quantity for a product (e.g. after a stock count or restock).
    /// This is a full replacement, not an increment — use <see cref="DeductStock"/> for
    /// order-driven deductions. Restricted to Admin and StoreManager roles.
    /// </summary>
    /// <param name="id">The product to update.</param>
    /// <param name="req">The new absolute stock quantity.</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Atomically deducts a specified quantity from a product's stock.
    /// Called by OrderService after a successful order placement to reduce inventory.
    /// Returns 400 if the requested quantity exceeds available stock, preventing
    /// overselling. This endpoint is intentionally open (no role restriction) because
    /// it is called service-to-service, not by end users.
    /// </summary>
    /// <param name="id">The product whose stock should be reduced.</param>
    /// <param name="req">The quantity to deduct.</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>
    /// Maps a <see cref="Product"/> domain entity to an anonymous response DTO.
    /// Computes the discounted price inline so the frontend always receives the
    /// final customer-facing price without needing to apply discount logic itself.
    /// </summary>
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

/// <summary>
/// HTTP API controller for product category management.
/// Categories form the top-level taxonomy of the product catalogue.
/// Read access is public; write access is restricted to Admin.
/// </summary>
[ApiController]
[Route("api/v1/categories")]
public class CategoriesController(IProductRepository repo) : ControllerBase
{
    /// <summary>
    /// Returns all product categories. Used to populate the category navigation
    /// menu and the category filter in the product search UI.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var cats = await repo.GetCategoriesAsync(ct);
        return Ok(cats.Select(c => new { c.Id, c.Name, c.Description, c.ImageUrl }));
    }

    /// <summary>
    /// Creates a new product category.
    /// Restricted to Admin role to prevent unauthorised taxonomy changes.
    /// </summary>
    /// <param name="req">The category name, optional description, and optional image URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 Created with the new category's ID and name.</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory(CreateCategoryRequest req, CancellationToken ct)
    {
        var cat = Category.Create(req.Name, req.Description, req.ImageUrl);
        await repo.AddCategoryAsync(cat, ct);
        return CreatedAtAction(nameof(GetCategories), new { id = cat.Id }, new { cat.Id, cat.Name });
    }
}

/// <summary>Request body for creating a new product.</summary>
public record CreateProductRequest(string Name, string Description, decimal Price, string Sku,
    string ImageUrl, Guid CategoryId, int StockQuantity, string? Brand, decimal? Weight, string? Unit);

/// <summary>Request body for a full product update (all mutable fields).</summary>
public record UpdateProductRequest(string Name, string Description, decimal Price, string ImageUrl,
    Guid CategoryId, string? Brand, string? Unit, decimal? Weight, decimal DiscountPercent, bool IsActive);

/// <summary>Request body for setting the absolute stock quantity.</summary>
public record UpdateStockRequest(int Quantity);

/// <summary>Request body for deducting a quantity from stock (used by OrderService).</summary>
public record DeductStockRequest(int Quantity);

/// <summary>Request body for updating only the discount percentage of a product.</summary>
public record UpdateDiscountRequest(decimal DiscountPercent);

/// <summary>Request body for creating a new product category.</summary>
public record CreateCategoryRequest(string Name, string? Description, string? ImageUrl);
