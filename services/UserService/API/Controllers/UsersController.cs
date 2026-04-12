using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Commands;
using UserService.Application.Queries;

namespace UserService.API.Controllers;

/// <summary>
/// HTTP API controller for admin-level user management.
/// Provides CRUD operations over the user base, role assignment, and
/// account activation/deactivation. All endpoints require the Admin role —
/// this controller is the back-office user management panel, not the
/// customer-facing profile API (which lives in AuthService).
/// Reads from and writes to the same <c>GroceryAuth</c> database as AuthService
/// via <see cref="UserService.Infrastructure.UserDbContext"/>, ensuring a single
/// source of truth for user data.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
public class UsersController(GetUsersHandler queries, UpdateUserHandler commands) : ControllerBase
{
    /// <summary>
    /// Returns a filtered list of all users. Supports filtering by role, active status,
    /// and a free-text search across email, first name, and last name.
    /// Used by the admin dashboard's user management table.
    /// </summary>
    /// <param name="role">Optional role filter (e.g. "Customer", "DeliveryDriver").</param>
    /// <param name="search">Optional search term matched against email and name fields.</param>
    /// <param name="isActive">Optional active status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? role, [FromQuery] string? search, [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var users = await queries.HandleAsync(new GetUsersQuery(role, search, isActive), ct);
        return Ok(users);
    }

    /// <summary>
    /// Returns aggregate statistics about the user base: total count, active/inactive
    /// split, and a breakdown by role. Used by the admin dashboard's summary cards.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct) =>
        Ok(await queries.GetStatsAsync(ct));

    /// <summary>
    /// Retrieves a single user by their unique identifier.
    /// Returns 404 if the user does not exist.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await queries.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Updates a user's profile fields (email, name, phone number).
    /// Email uniqueness is enforced — returns 409 Conflict if the new email
    /// is already in use by another account.
    /// </summary>
    /// <param name="id">The user to update.</param>
    /// <param name="req">The new field values (null fields are ignored).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var (success, error, result) = await commands.UpdateAsync(
            new UpdateUserCommand(id, req.Email, req.FirstName, req.LastName, req.PhoneNumber), ct);
        if (!success) return error == "User not found" ? NotFound() : Conflict(new { error });
        return Ok(result);
    }

    /// <summary>
    /// Changes a user's role (e.g. Customer → DeliveryDriver, Customer → StoreManager).
    /// Only the four defined roles are accepted; invalid role strings return 400.
    /// Role changes take effect on the user's next login (the JWT is not invalidated).
    /// </summary>
    /// <param name="id">The user whose role should change.</param>
    /// <param name="req">The new role string.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest req, CancellationToken ct)
    {
        var (success, error, result) = await commands.ChangeRoleAsync(new ChangeRoleCommand(id, req.Role), ct);
        if (!success) return error == "User not found" ? NotFound() : BadRequest(new { error });
        return Ok(result);
    }

    /// <summary>
    /// Toggles a user's active status between active and inactive.
    /// When deactivating, the user's refresh token is immediately revoked so they
    /// cannot silently obtain a new access token — they are effectively locked out
    /// on their next token refresh attempt.
    /// </summary>
    /// <param name="id">The user to activate or deactivate.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPatch("{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
    {
        var (success, result) = await commands.ToggleActiveAsync(new ToggleActiveCommand(id), ct);
        return success ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Permanently deletes a user account.
    /// Before deletion, the refresh token is revoked to invalidate any active sessions.
    /// This is a hard delete — the user record is removed from the database.
    /// Returns 204 No Content on success, 404 if the user does not exist.
    /// </summary>
    /// <param name="id">The user to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await commands.DeleteAsync(new DeleteUserCommand(id), ct);
        return deleted ? NoContent() : NotFound();
    }
}

/// <summary>Request body for updating a user's profile fields. Null fields are ignored.</summary>
public record UpdateUserRequest(string? Email, string? FirstName, string? LastName, string? PhoneNumber);

/// <summary>Request body for changing a user's role.</summary>
public record ChangeRoleRequest(string Role);
