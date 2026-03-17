namespace ProductService.Domain;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<(IEnumerable<Product> Items, int Total)> SearchAsync(
        string? query, Guid? categoryId, decimal? minPrice, decimal? maxPrice,
        string? sortBy, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task<IEnumerable<Product>> GetLowStockAsync(CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task<IEnumerable<Category>> GetCategoriesAsync(CancellationToken ct = default);
    Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default);
    Task AddCategoryAsync(Category category, CancellationToken ct = default);
}
