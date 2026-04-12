using Microsoft.EntityFrameworkCore;
using ReviewService.Infrastructure;

namespace ReviewService.Application.Queries;

/// <summary>
/// Query to retrieve all reviews for a specific product.
/// Used by both the public product page (anonymous) and the authenticated
/// "can-review" eligibility check.
/// </summary>
public record GetReviewsQuery(Guid ProductId);

/// <summary>
/// Read model DTO representing a single review as returned by the query handler.
/// Mirrors <see cref="ReviewService.Application.Commands.ReviewDto"/> but lives
/// in the Queries namespace to keep read and write models independent.
/// All IDs are strings for JSON compatibility; <c>CreatedAt</c> is ISO 8601.
/// </summary>
public record ReviewDto(
    string Id, string ProductId, string CustomerId,
    string CustomerName, int Rating, string? Comment, string CreatedAt);

/// <summary>
/// Query handler that retrieves all reviews for a product from the database,
/// ordered newest first. Directly queries the <see cref="ReviewDbContext"/>
/// using a LINQ projection to avoid loading the full domain entity when only
/// the read model fields are needed.
/// </summary>
public class GetReviewsHandler(ReviewDbContext db)
{
    /// <summary>
    /// Fetches all reviews for the given product, ordered by creation date descending,
    /// and projects them to <see cref="ReviewDto"/> objects.
    /// </summary>
    /// <param name="query">The query containing the product ID to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of review DTOs, newest first. Empty list if no reviews exist.</returns>
    public async Task<List<ReviewDto>> HandleAsync(GetReviewsQuery query, CancellationToken ct = default) =>
        await db.Reviews
            .Where(r => r.ProductId == query.ProductId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                r.Id.ToString(), r.ProductId.ToString(), r.CustomerId.ToString(),
                r.CustomerName, r.Rating, r.Comment, r.CreatedAt.ToString("o")))
            .ToListAsync(ct);
}
