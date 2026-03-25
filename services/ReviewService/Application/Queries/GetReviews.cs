using Microsoft.EntityFrameworkCore;
using ReviewService.Infrastructure;

namespace ReviewService.Application.Queries;

public record GetReviewsQuery(Guid ProductId);

public record ReviewDto(
    string Id, string ProductId, string CustomerId,
    string CustomerName, int Rating, string? Comment, string CreatedAt);

public class GetReviewsHandler(ReviewDbContext db)
{
    public async Task<List<ReviewDto>> HandleAsync(GetReviewsQuery query, CancellationToken ct = default) =>
        await db.Reviews
            .Where(r => r.ProductId == query.ProductId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                r.Id.ToString(), r.ProductId.ToString(), r.CustomerId.ToString(),
                r.CustomerName, r.Rating, r.Comment, r.CreatedAt.ToString("o")))
            .ToListAsync(ct);
}
