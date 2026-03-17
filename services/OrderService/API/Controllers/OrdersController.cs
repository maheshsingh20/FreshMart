using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;
using OrderService.Application.Queries;
using System.Security.Claims;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController(IMediator mediator) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCustomerOrdersQuery(CurrentUserId, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrderByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req, CancellationToken ct)
    {
        var cmd = new CreateOrderCommand(CurrentUserId, req.DeliveryAddress,
            req.Items.Select(i => new OrderItemRequest(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            req.Notes);
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return CreatedAtAction(nameof(GetOrder), new { id = result.Value }, new { id = result.Value });
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelOrderCommand(id, req.Reason), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }
}

public record CreateOrderRequest(string DeliveryAddress, List<OrderItemRequest> Items, string? Notes);
public record CancelRequest(string Reason);
