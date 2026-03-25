using SharedKernel.Domain;

namespace ProductService.Domain;

public class Product : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Price { get; private set; }
    public string SKU { get; private set; } = default!;
    public string ImageUrl { get; private set; } = default!;
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public int StockQuantity { get; private set; }
    public int LowStockThreshold { get; private set; } = 10;
    public bool IsActive { get; private set; } = true;
    public double AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public string? Brand { get; private set; }
    public decimal? Weight { get; private set; }
    public string? Unit { get; private set; } // kg, litre, piece
    public decimal DiscountPercent { get; private set; }

    private Product() { }

    public static Product Create(string name, string description, decimal price,
        string sku, string imageUrl, Guid categoryId, int initialStock,
        string? brand = null, decimal? weight = null, string? unit = null)
    {
        var product = new Product
        {
            Name = name,
            Description = description,
            Price = price,
            SKU = sku,
            ImageUrl = imageUrl,
            CategoryId = categoryId,
            StockQuantity = initialStock,
            Brand = brand,
            Weight = weight,
            Unit = unit
        };
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Name, product.Price));
        return product;
    }

    public void UpdateStock(int quantity)
    {
        var previous = StockQuantity;
        StockQuantity = quantity;
        SetUpdated();
        AddDomainEvent(new InventoryUpdatedDomainEvent(Id, quantity, previous));

        if (StockQuantity <= LowStockThreshold)
            AddDomainEvent(new LowStockDomainEvent(Id, Name, StockQuantity, LowStockThreshold));
    }

    public void DeductStock(int quantity)
    {
        if (StockQuantity < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {Name}.");
        UpdateStock(StockQuantity - quantity);
    }

    public void UpdatePrice(decimal price) { Price = price; SetUpdated(); }
    public void SetDiscount(decimal percent) { DiscountPercent = Math.Clamp(percent, 0, 100); SetUpdated(); }
    public void Deactivate() { IsActive = false; SetUpdated(); }
    public void Activate() { IsActive = true; SetUpdated(); }

    public void Update(string name, string description, decimal price, string imageUrl,
        Guid categoryId, string? brand, string? unit, decimal? weight, bool isActive)
    {
        Name = name; Description = description; Price = price;
        ImageUrl = imageUrl; CategoryId = categoryId;
        Brand = brand; Unit = unit; Weight = weight; IsActive = isActive;
        SetUpdated();
    }
}

public class Category : Entity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public ICollection<Product> Products { get; private set; } = [];

    private Category() { }

    public static Category Create(string name, string? description = null,
        string? imageUrl = null, Guid? parentId = null) =>
        new() { Name = name, Description = description, ImageUrl = imageUrl, ParentCategoryId = parentId };
}

public record ProductCreatedEvent(Guid ProductId, string Name, decimal Price) : DomainEvent
{
    public override string EventType => "ProductCreated";
}

public record InventoryUpdatedDomainEvent(Guid ProductId, int NewQty, int PreviousQty) : DomainEvent
{
    public override string EventType => "InventoryUpdated";
}

public record LowStockDomainEvent(Guid ProductId, string ProductName, int Stock, int Threshold) : DomainEvent
{
    public override string EventType => "LowStock";
}
