using Microsoft.EntityFrameworkCore;
using ProductService.Domain;

namespace ProductService.Infrastructure.Persistence;

public class ProductRepository(ProductDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.SKU == sku, ct);

    public async Task<(IEnumerable<Product> Items, int Total)> SearchAsync(
        string? query, Guid? categoryId, decimal? minPrice, decimal? maxPrice,
        string? sortBy, int page, int pageSize, CancellationToken ct = default,
        string? brand = null)
    {
        var q = db.Products.Include(p => p.Category).Where(p => p.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(p => p.Name.Contains(query) || p.Description.Contains(query) || (p.Brand != null && p.Brand.Contains(query)));
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId);
        if (minPrice.HasValue) q = q.Where(p => p.Price >= minPrice);
        if (maxPrice.HasValue) q = q.Where(p => p.Price <= maxPrice);
        if (!string.IsNullOrWhiteSpace(brand)) q = q.Where(p => p.Brand == brand);
        q = sortBy switch
        {
            "price_asc" => q.OrderBy(p => p.Price),
            "price_desc" => q.OrderByDescending(p => p.Price),
            "rating" => q.OrderByDescending(p => p.AverageRating),
            _ => q.OrderBy(p => p.Name)
        };
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        await db.Products.Include(p => p.Category).Where(p => p.CategoryId == categoryId && p.IsActive).ToListAsync(ct);

    public async Task<IEnumerable<Product>> GetLowStockAsync(CancellationToken ct = default) =>
        await db.Products.Where(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive).ToListAsync(ct);

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        db.Products.Update(product);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync(CancellationToken ct = default) =>
        await db.Categories.ToListAsync(ct);

    public Task<Category?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddCategoryAsync(Category category, CancellationToken ct = default)
    {
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
    }
}
