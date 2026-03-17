using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/v1/products/{productId}/reviews")]
public class ReviewsController(AppDbContext db) : ControllerBase
{
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User ID claim not found"));

    [HttpGet]
    public async Task<IActionResult> GetReviews(Guid productId)
    {
        var reviews = await db.Reviews
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(r.Id.ToString(), r.ProductId.ToString(), r.CustomerId.ToString(),
                r.CustomerName, r.Rating, r.Comment, r.CreatedAt.ToString("o")))
            .ToListAsync();
        return Ok(reviews);
    }

    [HttpGet("can-review")]
    [Authorize]
    public async Task<IActionResult> CanReview(Guid productId)
    {
        var hasDelivered = await db.Orders
            .AnyAsync(o => o.CustomerId == UserId && o.Status == "Delivered" &&
                           o.Items.Any(i => i.ProductId == productId));
        var alreadyReviewed = await db.Reviews
            .AnyAsync(r => r.ProductId == productId && r.CustomerId == UserId);
        return Ok(new { canReview = hasDelivered && !alreadyReviewed, alreadyReviewed });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview(Guid productId, CreateReviewRequest req)
    {
        if (req.Rating < 1 || req.Rating > 5)
            return BadRequest(new { error = "Rating must be between 1 and 5" });

        var hasDelivered = await db.Orders
            .AnyAsync(o => o.CustomerId == UserId && o.Status == "Delivered" &&
                           o.Items.Any(i => i.ProductId == productId));
        if (!hasDelivered)
            return BadRequest(new { error = "You can only review products from delivered orders" });

        var alreadyReviewed = await db.Reviews
            .AnyAsync(r => r.ProductId == productId && r.CustomerId == UserId);
        if (alreadyReviewed)
            return Conflict(new { error = "You have already reviewed this product" });

        var user = await db.Users.FindAsync(UserId);
        var review = new Review
        {
            ProductId = productId,
            CustomerId = UserId,
            CustomerName = $"{user!.FirstName} {user.LastName}",
            Rating = req.Rating,
            Comment = req.Comment
        };
        db.Reviews.Add(review);

        // Update product average rating
        var product = await db.Products.FindAsync(productId);
        if (product != null)
        {
            var allRatings = await db.Reviews.Where(r => r.ProductId == productId).Select(r => r.Rating).ToListAsync();
            allRatings.Add(req.Rating);
            product.AverageRating = allRatings.Average();
        }

        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetReviews), new { productId },
            new ReviewDto(review.Id.ToString(), review.ProductId.ToString(), review.CustomerId.ToString(),
                review.CustomerName, review.Rating, review.Comment, review.CreatedAt.ToString("o")));
    }
}
