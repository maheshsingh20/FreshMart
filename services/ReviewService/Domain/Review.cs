namespace ReviewService.Domain;

/// <summary>
/// Represents a customer's star rating and written review for a product.
/// Customers can only review products they have purchased (enforced by ReviewService
/// via a call to OrderService to verify purchase history).
/// </summary>
public class Review
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The product being reviewed.</summary>
    public Guid ProductId { get; set; }

    /// <summary>The customer who wrote the review.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Display name of the reviewer shown publicly.</summary>
    public string CustomerName { get; set; } = "";

    /// <summary>Star rating from 1 (worst) to 5 (best).</summary>
    public int Rating { get; set; }

    /// <summary>Optional written review text.</summary>
    public string? Comment { get; set; }

    /// <summary>UTC timestamp when the review was submitted.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
