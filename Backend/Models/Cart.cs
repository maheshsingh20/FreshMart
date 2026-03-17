namespace Backend.Models;

public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public AppUser Customer { get; set; } = null!;
    public decimal? BudgetLimit { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public ICollection<CartItem> Items { get; set; } = [];
}

public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}
