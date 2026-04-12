using Microsoft.EntityFrameworkCore;
using ReviewService.Domain;
using ReviewService.Infrastructure;

namespace ReviewService.Application.Commands;

/// <summary>
/// Command encapsulating everything needed to create a product review.
/// Carries the product and customer identifiers, the customer's display name
/// (snapshotted from the JWT at request time), the numeric rating, and an
/// optional free-text comment.
/// </summary>
public record CreateReviewCommand(
    Guid ProductId, Guid CustomerId, string CustomerName, int Rating, string? Comment);

/// <summary>
/// Application service handler for <see cref="CreateReviewCommand"/>.
/// Enforces two business rules before persisting:
/// <list type="number">
///   <item>Rating must be between 1 and 5 (inclusive).</item>
///   <item>A customer may only review a product once (one-review-per-customer-per-product).</item>
/// </list>
/// Returns a result tuple so the controller can map failures to appropriate
/// HTTP status codes (400 for validation, 409 for duplicate review).
/// </summary>
public class CreateReviewHandler(ReviewDbContext db)
{
    /// <summary>
    /// Validates the rating, checks for an existing review, persists the new review,
    /// and returns the created <see cref="ReviewDto"/>.
    /// </summary>
    /// <param name="cmd">The review command with all required fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A tuple of (Success, Error, Review). On success, Error is null and Review is populated.
    /// On failure, Success is false and Error contains a human-readable message.
    /// </returns>
    public async Task<(bool Success, string? Error, ReviewDto? Review)> HandleAsync(
        CreateReviewCommand cmd, CancellationToken ct = default)
    {
        if (cmd.Rating < 1 || cmd.Rating > 5)
            return (false, "Rating must be between 1 and 5", null);

        var exists = await db.Reviews.AnyAsync(
            r => r.ProductId == cmd.ProductId && r.CustomerId == cmd.CustomerId, ct);
        if (exists) return (false, "You have already reviewed this product", null);

        var review = new Review
        {
            ProductId = cmd.ProductId,
            CustomerId = cmd.CustomerId,
            CustomerName = cmd.CustomerName,
            Rating = cmd.Rating,
            Comment = cmd.Comment
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);

        return (true, null, new ReviewDto(
            review.Id.ToString(), review.ProductId.ToString(), review.CustomerId.ToString(),
            review.CustomerName, review.Rating, review.Comment, review.CreatedAt.ToString("o")));
    }
}

/// <summary>
/// Read model DTO representing a persisted review as returned by the API.
/// All IDs are serialised as strings for JSON compatibility with the Angular frontend.
/// <c>CreatedAt</c> is formatted as ISO 8601 (<c>o</c> format specifier) for
/// unambiguous timezone handling across client and server.
/// </summary>
public record ReviewDto(
    string Id, string ProductId, string CustomerId,
    string CustomerName, int Rating, string? Comment, string CreatedAt);
