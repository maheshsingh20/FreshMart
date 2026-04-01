namespace CartService.Domain;

/// <summary>
/// Aggregate root representing a customer's shopping cart.
/// Stored as a JSON blob in Redis keyed by <see cref="CustomerId"/>.
/// Cart state is ephemeral — it is cleared after a successful order is placed.
/// </summary>
public class Cart
{
    /// <summary>The customer who owns this cart.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Line items currently in the cart.</summary>
    public List<CartItem> Items { get; set; } = [];

    /// <summary>Optional spending cap set by the customer. Triggers <see cref="IsOverBudget"/> when exceeded.</summary>
    public decimal? BudgetLimit { get; set; }

    /// <summary>UTC timestamp of the last mutation — used for cache invalidation and stale-cart detection.</summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>Sum of all item <see cref="CartItem.TotalPrice"/> values (discounted prices × quantities).</summary>
    public decimal SubTotal => Items.Sum(i => i.TotalPrice);

    /// <summary><c>true</c> when <see cref="SubTotal"/> exceeds <see cref="BudgetLimit"/>.</summary>
    public bool IsOverBudget => BudgetLimit.HasValue && SubTotal > BudgetLimit;

    /// <summary>Total number of individual units across all line items.</summary>
    public int TotalItems => Items.Sum(i => i.Quantity);

    /// <summary>
    /// Adds a product to the cart. If the product already exists, increments its quantity.
    /// </summary>
    /// <param name="productId">Product to add.</param>
    /// <param name="name">Product name snapshot (stored so cart renders without calling ProductService).</param>
    /// <param name="price">Discounted unit price at the time of adding.</param>
    /// <param name="imageUrl">Product image URL snapshot.</param>
    /// <param name="quantity">Number of units to add.</param>
    /// <param name="originalPrice">Pre-discount price — used to show savings in checkout.</param>
    /// <param name="discountPercent">Discount percentage applied to this item.</param>
    public void AddItem(Guid productId, string name, decimal price, string imageUrl, int quantity = 1,
        decimal originalPrice = 0, decimal discountPercent = 0)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
            existing.Quantity += quantity;
        else
            Items.Add(new CartItem(productId, name, price, imageUrl, quantity, originalPrice, discountPercent));

        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>Removes a product from the cart entirely.</summary>
    public void RemoveItem(Guid productId)
    {
        Items.RemoveAll(i => i.ProductId == productId);
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the quantity of an existing item. Removes the item if <paramref name="quantity"/> is zero or negative.
    /// </summary>
    public void UpdateQuantity(Guid productId, int quantity)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null) return;

        if (quantity <= 0) RemoveItem(productId);
        else { item.Quantity = quantity; LastUpdated = DateTime.UtcNow; }
    }

    /// <summary>Removes all items from the cart.</summary>
    public void Clear()
    {
        Items.Clear();
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>Sets or clears the optional budget limit.</summary>
    public void SetBudget(decimal? budget)
    {
        BudgetLimit = budget;
        LastUpdated = DateTime.UtcNow;
    }
}

/// <summary>
/// A single product line item within a <see cref="Cart"/>.
/// Stores both the discounted price and the original price so the checkout page
/// can display savings without calling ProductService again.
/// </summary>
public class CartItem(Guid productId, string name, decimal price, string imageUrl, int quantity,
    decimal originalPrice = 0, decimal discountPercent = 0)
{
    /// <summary>Product identifier.</summary>
    public Guid ProductId { get; set; } = productId;

    /// <summary>Product name snapshot at the time the item was added.</summary>
    public string ProductName { get; set; } = name;

    /// <summary>Discounted unit price (what the customer actually pays per unit).</summary>
    public decimal UnitPrice { get; set; } = price;

    /// <summary>Product image URL snapshot.</summary>
    public string ImageUrl { get; set; } = imageUrl;

    /// <summary>Number of units in the cart.</summary>
    public int Quantity { get; set; } = quantity;

    /// <summary>Pre-discount unit price. Equals <see cref="UnitPrice"/> when no discount applies.</summary>
    public decimal OriginalPrice { get; set; } = originalPrice > 0 ? originalPrice : price;

    /// <summary>Discount percentage applied (0–100). Zero means no discount.</summary>
    public decimal DiscountPercent { get; set; } = discountPercent;

    /// <summary>Total cost at discounted price: <see cref="UnitPrice"/> × <see cref="Quantity"/>.</summary>
    public decimal TotalPrice => UnitPrice * Quantity;

    /// <summary>Total cost at original price: <see cref="OriginalPrice"/> × <see cref="Quantity"/>.</summary>
    public decimal OriginalTotalPrice => OriginalPrice * Quantity;
}
