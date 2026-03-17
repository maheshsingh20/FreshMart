namespace CartService.Domain;

public class Cart
{
    public Guid CustomerId { get; set; }
    public List<CartItem> Items { get; set; } = [];
    public decimal? BudgetLimit { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public decimal SubTotal => Items.Sum(i => i.TotalPrice);
    public bool IsOverBudget => BudgetLimit.HasValue && SubTotal > BudgetLimit;
    public int TotalItems => Items.Sum(i => i.Quantity);

    public void AddItem(Guid productId, string name, decimal price, string imageUrl, int quantity = 1)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
            existing.Quantity += quantity;
        else
            Items.Add(new CartItem(productId, name, price, imageUrl, quantity));

        LastUpdated = DateTime.UtcNow;
    }

    public void RemoveItem(Guid productId)
    {
        Items.RemoveAll(i => i.ProductId == productId);
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateQuantity(Guid productId, int quantity)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null) return;

        if (quantity <= 0) RemoveItem(productId);
        else { item.Quantity = quantity; LastUpdated = DateTime.UtcNow; }
    }

    public void Clear()
    {
        Items.Clear();
        LastUpdated = DateTime.UtcNow;
    }

    public void SetBudget(decimal? budget)
    {
        BudgetLimit = budget;
        LastUpdated = DateTime.UtcNow;
    }
}

public class CartItem(Guid productId, string name, decimal price, string imageUrl, int quantity)
{
    public Guid ProductId { get; set; } = productId;
    public string ProductName { get; set; } = name;
    public decimal UnitPrice { get; set; } = price;
    public string ImageUrl { get; set; } = imageUrl;
    public int Quantity { get; set; } = quantity;
    public decimal TotalPrice => UnitPrice * Quantity;
}
