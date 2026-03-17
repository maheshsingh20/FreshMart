namespace Backend.Models;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public double AverageRating { get; set; }
    public string? Brand { get; set; }
    public string? Unit { get; set; }
    public decimal DiscountPercent { get; set; } = 0; // 0-100
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
