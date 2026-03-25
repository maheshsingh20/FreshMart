using ReviewService.Application.Commands;
using ReviewService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ReviewService.API.Controllers;

[ApiController]
[Route("api/v1/products/{productId}/reviews")]
public class ReviewsController(
    GetReviewsHandler getHandler,
    CreateReviewHandler createHandler,
    IHttpClientFactory http,
    IConfiguration config) : ControllerBase
{
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetReviews(Guid productId, CancellationToken ct)
    {
        var reviews = await getHandler.HandleAsync(new GetReviewsQuery(productId), ct);
        return Ok(reviews);
    }

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

public record CreateReviewRequest(int Rating, string? Comment);
public record OrderPage(List<OrderSummary>? Orders, int Total);
public record OrderSummary(string Id, string Status, List<OrderItem> Items);
public record OrderItem(string ProductId, string ProductName, int Quantity);
