using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportService.Application.Commands;
using SupportService.Application.Queries;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SupportService.API.Controllers;

[ApiController]
[Route("api/v1/support")]
[Authorize]
public class SupportController(GetTicketsHandler queries, TicketCommandHandler commands) : ControllerBase
{
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string UserName => ((User.FindFirstValue("firstName") ?? "") + " " + (User.FindFirstValue("lastName") ?? "")).Trim();
    private string UserEmail => User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";
    private string UserRole => User.FindFirstValue(ClaimTypes.Role) ?? "Customer";
    private bool IsStaff => UserRole is "Admin" or "StoreManager";

    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets(CancellationToken ct)
    {
        var (_, _, result) = await queries.GetAllAsync(new GetTicketsQuery(UserId, IsStaff), ct);
        return Ok(result);
    }

    [HttpGet("tickets/{id:guid}")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken ct)
    {
        var (success, error, result) = await queries.GetByIdAsync(new GetTicketByIdQuery(id, UserId, IsStaff), ct);
        if (!success) return error == "Forbidden" ? Forbid() : NotFound();
        return Ok(result);
    }

    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest req, CancellationToken ct)
    {
        var (_, result) = await commands.CreateAsync(
            new CreateTicketCommand(UserId, UserName, UserEmail, req.Subject, req.Category, req.Description, req.Priority), ct);
        return CreatedAtAction(nameof(GetTicket), new { id = (result as dynamic)!.Id }, result);
    }

    [HttpPost("tickets/{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest req, CancellationToken ct)
    {
        var (success, error, result) = await commands.SendMessageAsync(
            new SendMessageCommand(id, UserId, UserName, UserRole, req.Message, IsStaff), ct);
        if (!success) return NotFound(new { error });
        return Ok(result);
    }

    [HttpPatch("tickets/{id:guid}/status")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req, CancellationToken ct)
    {
        var (success, error) = await commands.UpdateStatusAsync(new UpdateTicketStatusCommand(id, req.Status), ct);
        if (!success) return NotFound(new { error });
        return NoContent();
    }
}

public record CreateTicketRequest(string Subject, string? Category, string? Description, string? Priority);
public record SendMessageRequest(string Message);
public record UpdateStatusRequest(string Status);
