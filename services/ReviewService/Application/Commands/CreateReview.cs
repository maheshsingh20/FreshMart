using Microsoft.EntityFrameworkCore;
using ReviewService.Domain;
using ReviewService.Infrastructure;

namespace ReviewService.Application.Commands;

public record CreateReviewCommand(
    Guid ProductId, Guid CustomerId, string CustomerName, int Rating, string? Comment);

public class CreateReviewHandler(ReviewDbContext db)
{
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

public record ReviewDto(
    string Id, string ProductId, string CustomerId,
    string CustomerName, int Rating, string? Comment, string CreatedAt);
