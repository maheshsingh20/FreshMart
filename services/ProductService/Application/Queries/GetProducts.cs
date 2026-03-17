using MediatR;
using ProductService.Domain;
using SharedKernel.CQRS;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace ProductService.Application.Queries;

// DTOs
public record ProductDto(Guid Id, string Name, string Description, decimal Price,
    string SKU, string ImageUrl, string CategoryName, int StockQuantity,
    bool IsActive, double AverageRating, string? Brand, string? Unit);

public record ProductListResponse(IEnumerable<ProductDto> Items, int Total, int Page, int PageSize);

public record CategoryDto(Guid Id, string Name, string? Description, string? ImageUrl, Guid? ParentCategoryId);

// Queries
public record GetProductsQuery(
    string? Query, Guid? CategoryId, decimal? MinPrice, decimal? MaxPrice,
    string? SortBy, int Page = 1, int PageSize = 20) : IQuery<ProductListResponse>;

public record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto?>;
public record GetCategoriesQuery : IQuery<IEnumerable<CategoryDto>>;
public record GetLowStockProductsQuery : IQuery<IEnumerable<ProductDto>>;

// Handlers
public class GetProductsHandler(IProductRepository repo, IConnectionMultiplexer redis)
    : IQueryHandler<GetProductsQuery, ProductListResponse>
{
    public async Task<ProductListResponse> Handle(GetProductsQuery q, CancellationToken ct)
    {
        var cacheKey = $"products:{q.Query}:{q.CategoryId}:{q.MinPrice}:{q.MaxPrice}:{q.SortBy}:{q.Page}:{q.PageSize}";
        var db = redis.GetDatabase();
        var cached = await db.StringGetAsync(cacheKey);

        if (cached.HasValue)
            return JsonConvert.DeserializeObject<ProductListResponse>(cached!)!;

        var (items, total) = await repo.SearchAsync(
            q.Query, q.CategoryId, q.MinPrice, q.MaxPrice, q.SortBy, q.Page, q.PageSize, ct);

        var response = new ProductListResponse(
            items.Select(MapToDto), total, q.Page, q.PageSize);

        await db.StringSetAsync(cacheKey, JsonConvert.SerializeObject(response), TimeSpan.FromMinutes(5));
        return response;
    }

    private static ProductDto MapToDto(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.SKU, p.ImageUrl,
            p.Category?.Name ?? "", p.StockQuantity, p.IsActive, p.AverageRating, p.Brand, p.Unit);
}

public class GetProductByIdHandler(IProductRepository repo, IConnectionMultiplexer redis)
    : IQueryHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery q, CancellationToken ct)
    {
        var cacheKey = $"product:{q.ProductId}";
        var db = redis.GetDatabase();
        var cached = await db.StringGetAsync(cacheKey);

        if (cached.HasValue)
            return JsonConvert.DeserializeObject<ProductDto>(cached!)!;

        var product = await repo.GetByIdAsync(q.ProductId, ct);
        if (product is null) return null;

        var dto = new ProductDto(product.Id, product.Name, product.Description, product.Price,
            product.SKU, product.ImageUrl, product.Category?.Name ?? "",
            product.StockQuantity, product.IsActive, product.AverageRating, product.Brand, product.Unit);

        await db.StringSetAsync(cacheKey, JsonConvert.SerializeObject(dto), TimeSpan.FromMinutes(10));
        return dto;
    }
}

public class GetCategoriesHandler(IProductRepository repo)
    : IQueryHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
{
    public async Task<IEnumerable<CategoryDto>> Handle(GetCategoriesQuery q, CancellationToken ct)
    {
        var cats = await repo.GetCategoriesAsync(ct);
        return cats.Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.ImageUrl, c.ParentCategoryId));
    }
}

public class GetLowStockHandler(IProductRepository repo)
    : IQueryHandler<GetLowStockProductsQuery, IEnumerable<ProductDto>>
{
    public async Task<IEnumerable<ProductDto>> Handle(GetLowStockProductsQuery q, CancellationToken ct)
    {
        var products = await repo.GetLowStockAsync(ct);
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price,
            p.SKU, p.ImageUrl, p.Category?.Name ?? "", p.StockQuantity,
            p.IsActive, p.AverageRating, p.Brand, p.Unit));
    }
}

public record SuggestionDto(Guid Id, string Name, string ImageUrl, string CategoryName, decimal Price);
public record GetSuggestionsQuery(string? SearchQuery, string? ProductIds) : IQuery<IEnumerable<SuggestionDto>>;

public class GetSuggestionsHandler(IProductRepository repo)
    : IQueryHandler<GetSuggestionsQuery, IEnumerable<SuggestionDto>>
{
    public async Task<IEnumerable<SuggestionDto>> Handle(GetSuggestionsQuery q, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(q.SearchQuery))
        {
            var (items, _) = await repo.SearchAsync(q.SearchQuery, null, null, null, null, 1, 6, ct);
            return items.Select(p => new SuggestionDto(p.Id, p.Name, p.ImageUrl, p.Category?.Name ?? "", p.Price));
        }
        return [];
    }
}
