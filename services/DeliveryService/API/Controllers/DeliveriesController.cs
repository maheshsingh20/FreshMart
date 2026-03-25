using DeliveryService.Application;
using DeliveryService.Domain;
using DeliveryService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DeliveryService.API.Controllers;

[ApiController]
[Route("api/v1/deliveries")]
[Authorize]
public class DeliveriesController(IDeliveryAppService svc, IDeliveryRepository repo) : ControllerBase
{
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrder(Guid orderId, CancellationToken ct)
    {
        var d = await svc.GetByOrderIdAsync(orderId, ct);
        return d is null ? NotFound() : Ok(d);
    }

    [HttpGet("my")]
    [Authorize(Roles = "DeliveryDriver")]
    public async Task<IActionResult> GetMyDeliveries(CancellationToken ct)
    {
        var deliveries = await repo.GetByDriverAsync(UserId, ct);
        return Ok(deliveries.Select(MapDto));
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var deliveries = await repo.GetPendingAsync(ct);
        return Ok(deliveries.Select(MapDto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> CreateDelivery(CreateDeliveryRequest req, CancellationToken ct)
    {
        var delivery = Delivery.Create(req.OrderId, req.DeliveryAddress);
        await repo.AddAsync(delivery, ct);
        return CreatedAtAction(nameof(GetByOrder), new { orderId = req.OrderId }, MapDto(delivery));
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> AssignDriver(Guid id, AssignDriverRequest req, CancellationToken ct)
    {
        var result = await svc.AssignDriverAsync(id, req.DriverId, req.EstimatedDelivery, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "DeliveryDriver,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateStatusRequest req, CancellationToken ct)
    {
        var result = await svc.UpdateStatusAsync(id, req.Status, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    [HttpPatch("{id:guid}/location")]
    [Authorize(Roles = "DeliveryDriver")]
    public async Task<IActionResult> UpdateLocation(Guid id, UpdateLocationRequest req, CancellationToken ct)
    {
        await svc.UpdateLocationAsync(id, req.Lat, req.Lng, ct);
        return NoContent();
    }

    [HttpGet("slots")]
    public async Task<IActionResult> GetSlots([FromQuery] DateTime date, CancellationToken ct)
    {
        var slots = await svc.GetAvailableSlotsAsync(date, ct);
        return Ok(slots);
    }

    private static object MapDto(Delivery d) => new
    {
        d.Id, d.OrderId, d.DriverId, status = d.Status.ToString(),
        d.DeliveryAddress, lat = d.CurrentLatitude, lng = d.CurrentLongitude,
        d.ScheduledAt, d.EstimatedDelivery, d.ActualDelivery
    };
}

public record CreateDeliveryRequest(Guid OrderId, string DeliveryAddress);
public record AssignDriverRequest(Guid DriverId, DateTime EstimatedDelivery);
public record UpdateStatusRequest(string Status);
public record UpdateLocationRequest(double Lat, double Lng);
