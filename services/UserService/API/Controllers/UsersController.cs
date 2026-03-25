using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Commands;
using UserService.Application.Queries;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
public class UsersController(GetUsersHandler queries, UpdateUserHandler commands) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? role, [FromQuery] string? search, [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var users = await queries.HandleAsync(new GetUsersQuery(role, search, isActive), ct);
        return Ok(users);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct) =>
        Ok(await queries.GetStatsAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await queries.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var (success, error, result) = await commands.UpdateAsync(
            new UpdateUserCommand(id, req.Email, req.FirstName, req.LastName, req.PhoneNumber), ct);
        if (!success) return error == "User not found" ? NotFound() : Conflict(new { error });
        return Ok(result);
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest req, CancellationToken ct)
    {
        var (success, error, result) = await commands.ChangeRoleAsync(new ChangeRoleCommand(id, req.Role), ct);
        if (!success) return error == "User not found" ? NotFound() : BadRequest(new { error });
        return Ok(result);
    }

    [HttpPatch("{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
    {
        var (success, result) = await commands.ToggleActiveAsync(new ToggleActiveCommand(id), ct);
        return success ? Ok(result) : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await commands.DeleteAsync(new DeleteUserCommand(id), ct);
        return deleted ? NoContent() : NotFound();
    }
}

public record UpdateUserRequest(string? Email, string? FirstName, string? LastName, string? PhoneNumber);
public record ChangeRoleRequest(string Role);
