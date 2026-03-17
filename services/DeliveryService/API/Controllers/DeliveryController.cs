using DeliveryService.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryService.API.Controllers;

[ApiController]
[Route("api/v1/deliveries")]
[Authorize]
public class DeliveryController(IDeliveryAppService deliveryService) : ControllerBase
{
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetDelivery(Guid orderId, CancellationToken ct)
    {
        var delivery = await deliveryService.GetByOrderIdAsync(orderId, ct);
        return delivery is null ? NotFound() : Ok(delivery);
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> AssignDriver(Guid id, [FromBody] AssignDriverRequest req, CancellationToken ct)
    {
        var result = await deliveryService.AssignDriverAsync(id, req.DriverId, req.EstimatedDelivery, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok();
    }

    [HttpPut("{id:guid}/location")]
    [Authorize(Roles = "DeliveryDriver")]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] LocationUpdate req, CancellationToken ct)
    {
        await deliveryService.UpdateLocationAsync(id, req.Latitude, req.Longitude, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "DeliveryDriver,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] StatusUpdate req, CancellationToken ct)
    {
        var result = await deliveryService.UpdateStatusAsync(id, req.Status, ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    [HttpGet("slots")]
    public async Task<IActionResult> GetAvailableSlots([FromQuery] DateTime date, CancellationToken ct)
    {
        var slots = await deliveryService.GetAvailableSlotsAsync(date, ct);
        return Ok(slots);
    }
}

public record AssignDriverRequest(Guid DriverId, DateTime EstimatedDelivery);
public record LocationUpdate(double Latitude, double Longitude);
public record StatusUpdate(string Status);
