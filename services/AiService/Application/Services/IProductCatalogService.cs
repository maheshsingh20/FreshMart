namespace AiService.Application.Services;

public interface IProductCatalogService
{
    Task<List<CatalogProduct>> GetAllAsync(CancellationToken ct = default);
}

public record CatalogProduct(
    string Id, string Name, decimal Price, decimal DiscountPercent,
    string? Category, string? Brand, string? Unit);
