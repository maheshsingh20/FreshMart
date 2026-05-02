namespace AiService.Application.Services;

/// <summary>
/// Contract for fetching the product catalog from ProductService.
/// Implemented by <see cref="AiService.Infrastructure.ProductCatalogService"/>.
///
/// WHY AN INTERFACE:
///   Decouples the controller from the HTTP implementation.
///   In tests, you can return a fixed list of products without calling ProductService.
///
/// WHY A SEPARATE CATALOG MODEL:
///   The full Product model has many fields (SKU, weight, images, etc.) the AI doesn't need.
///   CatalogProduct is a minimal DTO with only what matters for building the AI prompt.
/// </summary>
public interface IProductCatalogService
{
    /// <summary>
    /// Fetches all active products from ProductService.
    /// Returns an empty list (never throws) if ProductService is unavailable —
    /// the AI can still respond, just without product context.
    /// </summary>
    Task<List<CatalogProduct>> GetAllAsync(CancellationToken ct = default);
}

/// <summary>
/// Minimal product data needed by the AI to build prompts and match ingredients.
/// Only contains fields relevant to the AI — not the full product model.
/// </summary>
/// <param name="Id">Product GUID — used in suggestedProducts so frontend can navigate to product page.</param>
/// <param name="Name">Product name — injected into AI prompt and matched against recipe ingredients.</param>
/// <param name="Price">Current price in INR — shown in AI responses.</param>
/// <param name="DiscountPercent">Discount % (0 if none) — AI mentions deals when applicable.</param>
/// <param name="Category">Category name e.g. "Dairy, Bread and Eggs" — helps AI give contextual suggestions.</param>
/// <param name="Brand">Brand name e.g. "Amul" — included in catalog text for the AI.</param>
/// <param name="Unit">Unit of measurement e.g. "1L", "500g" — shown in recipe ingredient matching.</param>
public record CatalogProduct(
    string Id, string Name, decimal Price, decimal DiscountPercent,
    string? Category, string? Brand, string? Unit);
