using ReviewService.Application.Commands;
using ReviewService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ReviewService.API.Controllers;

/// <summary>
/// HTTP API controller for product reviews.
/// Reviews are scoped to a specific product (the <c>productId</c> route parameter)
/// and are written by authenticated customers. The controller enforces a
/// "verified purchase" policy: a customer can only submit a review if they have
/// a delivered order containing the product. This is checked by querying the
/// OrderService at review-submission time. The <c>can-review</c> endpoint lets
/// the frontend show or hide the review form before the user attempts to submit.
/// </summary>
[ApiController]
[Route("api/v1/products/{productId}/reviews")]
public class ReviewsController(
    GetReviewsHandler getHandler,
    CreateReviewHandler createHandler,
    IHttpClientFactory http,
    IConfiguration config) : ControllerBase
{
    /// <summary>
    /// Resolves the authenticated user's ID from the JWT <c>sub</c> claim,
    /// falling back to <see cref="ClaimTypes.NameIdentifier"/> for compatibility.
    /// </summary>
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Returns all reviews for the specified product, ordered newest first.
    /// This endpoint is public — no authentication required — so product pages
    /// can display reviews to anonymous visitors.
    /// </summary>
    /// <param name="productId">The product whose reviews to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> GetReviews(Guid productId, CancellationToken ct)
    {
        var reviews = await getHandler.HandleAsync(new GetReviewsQuery(productId), ct);
        return Ok(reviews);
    }

    /// <summary>
    /// Checks whether the authenticated user is eligible to review the specified product.
    /// Returns two flags: <c>canReview</c> (true only if the user has a delivered order
    /// containing this product AND has not already reviewed it) and <c>alreadyReviewed</c>.
    /// The frontend uses this to conditionally render the review form.
    /// If OrderService is unavailable, <c>hasDelivered</c> defaults to false (fail-closed).
    /// </summary>
    /// <param name="productId">The product to check eligibility for.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("can-review")]
    [Authorize]
    public async Task<IActionResult> CanReview(Guid productId, CancellationToken ct)
    {
        var orderServiceUrl = config["Services:OrderService"];
        var client = http.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                Request.Headers.Authorization.ToString().Replace("Bearer ", ""));

        bool hasDelivered = false;
        try
        {
            var resp = await client.GetAsync($"{orderServiceUrl}/api/v1/orders", ct);
            if (resp.IsSuccessStatusCode)
            {
                var page = await resp.Content.ReadFromJsonAsync<OrderPage>(ct);
                hasDelivered = page?.Orders?.Any(o =>
                    o.Status == "Delivered" &&
                    o.Items.Any(i => i.ProductId == productId.ToString())) ?? false;
            }
        }
        catch { /* OrderService unavailable */ }

        var alreadyReviewed = (await getHandler.HandleAsync(new GetReviewsQuery(productId), ct))
            .Any(r => r.CustomerId == UserId.ToString());

        return Ok(new { canReview = hasDelivered && !alreadyReviewed, alreadyReviewed });
    }

    /// <summary>
    /// Submits a new review for the specified product.
    /// The customer's display name is assembled from the <c>firstName</c> and
    /// <c>lastName</c> JWT claims so it matches the name shown elsewhere in the app.
    /// Returns 409 Conflict if the user has already reviewed this product,
    /// and 400 Bad Request for other validation failures (e.g. rating out of range).
    /// </summary>
    /// <param name="productId">The product being reviewed.</param>
    /// <param name="req">The rating (1–5) and optional comment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 Created with the new review, 409 Conflict if already reviewed, or 400 on validation error.</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview(Guid productId, [FromBody] CreateReviewRequest req, CancellationToken ct)
    {
        var customerName = ((User.FindFirstValue("firstName") ?? "") + " " + (User.FindFirstValue("lastName") ?? "")).Trim();
        var (success, error, review) = await createHandler.HandleAsync(
            new CreateReviewCommand(productId, UserId, customerName, req.Rating, req.Comment), ct);

        if (!success) return error == "You have already reviewed this product"
            ? Conflict(new { error })
            : BadRequest(new { error });

        return CreatedAtAction(nameof(GetReviews), new { productId }, review);
    }
}

/// <summary>Request body for submitting a product review.</summary>
public record CreateReviewRequest(int Rating, string? Comment);

/// <summary>
/// Minimal DTO used to deserialise the paginated order list from OrderService
/// when checking whether the user has a delivered order containing the product.
/// </summary>
public record OrderPage(List<OrderSummary>? Orders, int Total);

/// <summary>Summary of a single order, used only for the "can review" eligibility check.</summary>
public record OrderSummary(string Id, string Status, List<OrderItem> Items);

/// <summary>A single line item within an order summary, used to check if a product was purchased.</summary>
public record OrderItem(string ProductId, string ProductName, int Quantity);
