using Microsoft.EntityFrameworkCore;
using ProductService.Domain;

namespace ProductService.Infrastructure.Persistence;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.Description).HasMaxLength(500);
            e.Property(c => c.ImageUrl).HasMaxLength(1000);
        });

        mb.Entity<Product>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Description).HasMaxLength(1000);
            e.Property(p => p.Price).HasColumnType("decimal(18,2)");
            e.Property(p => p.SKU).HasMaxLength(50).IsRequired();
            e.HasIndex(p => p.SKU).IsUnique();
            e.Property(p => p.ImageUrl).HasMaxLength(1000);
            e.Property(p => p.Brand).HasMaxLength(100);
            e.Property(p => p.Unit).HasMaxLength(50);
            e.Property(p => p.Weight).HasColumnType("decimal(10,3)");
            e.Property(p => p.DiscountPercent).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
            e.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId);
        });
    }
}
