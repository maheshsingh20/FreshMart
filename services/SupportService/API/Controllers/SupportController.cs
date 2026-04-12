using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportService.Application.Commands;
using SupportService.Application.Queries;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SupportService.API.Controllers;

/// <summary>
/// HTTP API controller for customer support ticket management.
/// Provides endpoints for customers to open tickets, send messages, and view
/// their own ticket history. Staff members (Admin, StoreManager) can view all
/// tickets and update ticket statuses. Real-time message delivery is handled
/// via SignalR (see <c>SupportHub</c>) — this controller handles the REST
/// operations while SignalR handles the live chat experience.
/// </summary>
[ApiController]
[Route("api/v1/support")]
[Authorize]
public class SupportController(GetTicketsHandler queries, TicketCommandHandler commands) : ControllerBase
{
    /// <summary>
    /// The authenticated user's ID, extracted from the JWT <c>sub</c> claim.
    /// Used to scope ticket queries to the current user for non-staff callers.
    /// </summary>
    private Guid UserId => Guid.Parse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// The authenticated user's full name, assembled from <c>firstName</c> and
    /// <c>lastName</c> JWT claims. Stored on messages so the chat history shows
    /// who sent each message even if the user's name changes later.
    /// </summary>
    private string UserName => ((User.FindFirstValue("firstName") ?? "") + " " + (User.FindFirstValue("lastName") ?? "")).Trim();

    /// <summary>The authenticated user's email, used when creating a new ticket.</summary>
    private string UserEmail => User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";

    /// <summary>The authenticated user's role, used to determine staff vs. customer access.</summary>
    private string UserRole => User.FindFirstValue(ClaimTypes.Role) ?? "Customer";

    /// <summary>
    /// <c>true</c> if the current user is Admin or StoreManager, granting access
    /// to all tickets rather than just their own.
    /// </summary>
    private bool IsStaff => UserRole is "Admin" or "StoreManager";

    /// <summary>
    /// Returns tickets visible to the current user.
    /// Customers see only their own tickets; staff see all tickets across all customers.
    /// Results are ordered newest first.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets(CancellationToken ct)
    {
        var (_, _, result) = await queries.GetAllAsync(new GetTicketsQuery(UserId, IsStaff), ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single ticket by ID, including its full message history.
    /// Customers can only access their own tickets; staff can access any ticket.
    /// Returns 403 Forbidden if a customer attempts to access another user's ticket,
    /// and 404 Not Found if the ticket does not exist.
    /// </summary>
    /// <param name="id">The ticket's unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("tickets/{id:guid}")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken ct)
    {
        var (success, error, result) = await queries.GetByIdAsync(new GetTicketByIdQuery(id, UserId, IsStaff), ct);
        if (!success) return error == "Forbidden" ? Forbid() : NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Opens a new support ticket on behalf of the authenticated customer.
    /// The ticket is created in the "Open" status. If a description is provided,
    /// it is added as the first message in the ticket's conversation thread.
    /// Returns 201 Created with the new ticket and a Location header.
    /// </summary>
    /// <param name="req">Subject, category, description, and priority for the new ticket.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest req, CancellationToken ct)
    {
        var (_, result) = await commands.CreateAsync(
            new CreateTicketCommand(UserId, UserName, UserEmail, req.Subject, req.Category, req.Description, req.Priority), ct);
        return CreatedAtAction(nameof(GetTicket), new { id = (result as dynamic)!.Id }, result);
    }

    /// <summary>
    /// Sends a message in an existing ticket's conversation thread.
    /// The message is persisted and immediately broadcast to all SignalR clients
    /// subscribed to the ticket's group, enabling real-time chat.
    /// If a staff member sends the first reply to an Open ticket, the ticket
    /// status is automatically advanced to "InProgress".
    /// </summary>
    /// <param name="id">The ticket to send the message to.</param>
    /// <param name="req">The message text.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("tickets/{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest req, CancellationToken ct)
    {
        var (success, error, result) = await commands.SendMessageAsync(
            new SendMessageCommand(id, UserId, UserName, UserRole, req.Message, IsStaff), ct);
        if (!success) return NotFound(new { error });
        return Ok(result);
    }

    /// <summary>
    /// Updates the status of a support ticket (e.g. Open → InProgress → Resolved).
    /// Restricted to Admin and StoreManager roles. When a ticket is resolved,
    /// the <c>ResolvedAt</c> timestamp is recorded for SLA tracking.
    /// The status change is broadcast via SignalR so the customer's UI updates in real time.
    /// </summary>
    /// <param name="id">The ticket whose status should be updated.</param>
    /// <param name="req">The new status string.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPatch("tickets/{id:guid}/status")]
    [Authorize(Roles = "Admin,StoreManager")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req, CancellationToken ct)
    {
        var (success, error) = await commands.UpdateStatusAsync(new UpdateTicketStatusCommand(id, req.Status), ct);
        if (!success) return NotFound(new { error });
        return NoContent();
    }
}

/// <summary>Request body for opening a new support ticket.</summary>
public record CreateTicketRequest(string Subject, string? Category, string? Description, string? Priority);

/// <summary>Request body for sending a message in a ticket conversation.</summary>
public record SendMessageRequest(string Message);

/// <summary>Request body for updating a ticket's status.</summary>
public record UpdateStatusRequest(string Status);
