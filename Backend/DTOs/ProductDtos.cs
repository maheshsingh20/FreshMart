namespace Backend.DTOs;

public record ProductDto(
    string Id, string Name, string Description, decimal Price, string Sku,
    string ImageUrl, string CategoryName, int StockQuantity, bool IsActive,
    double AverageRating, string? Brand, string? Unit,
    decimal DiscountPercent, decimal DiscountedPrice);

public record CategoryDto(string Id, string Name, string? Description, string? ImageUrl, string? ParentCategoryId);

public record PaginatedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);

public record CreateProductRequest(
    string Name, string Description, decimal Price, string Sku,
    string ImageUrl, Guid CategoryId, int StockQuantity, string? Brand, string? Unit);

public record UpdateStockRequest(int Quantity);
public record UpdateDiscountRequest(decimal DiscountPercent);
