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
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
public class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? role, [FromQuery] string? search, [FromQuery] bool? isActive)
    {
        var q = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(role)) q = q.Where(u => u.Role == role);
        if (isActive.HasValue) q = q.Where(u => u.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            q = q.Where(u => u.Email.Contains(s) || u.FirstName.Contains(s) || u.LastName.Contains(s));
        }
        var users = await q.OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserAdminDto(u.Id.ToString(), u.Email, u.FirstName, u.LastName, u.Role, u.PhoneNumber, u.IsActive, u.CreatedAt))
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var total = await db.Users.CountAsync();
        var byRole = await db.Users.GroupBy(u => u.Role)
            .Select(g => new { role = g.Key, count = g.Count() })
            .ToListAsync();
        var active = await db.Users.CountAsync(u => u.IsActive);
        return Ok(new { total, active, inactive = total - active, byRole });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        return Ok(new UserAdminDto(u.Id.ToString(), u.Email, u.FirstName, u.LastName, u.Role, u.PhoneNumber, u.IsActive, u.CreatedAt));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest req)
    {
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req.Email) && req.Email != u.Email)
        {
            if (await db.Users.AnyAsync(x => x.Email == req.Email.ToLower() && x.Id != id))
                return Conflict(new { error = "Email already in use" });
            u.Email = req.Email.ToLower();
        }
        if (!string.IsNullOrWhiteSpace(req.FirstName)) u.FirstName = req.FirstName;
        if (!string.IsNullOrWhiteSpace(req.LastName)) u.LastName = req.LastName;
        u.PhoneNumber = req.PhoneNumber;
        await db.SaveChangesAsync();
        return Ok(new UserAdminDto(u.Id.ToString(), u.Email, u.FirstName, u.LastName, u.Role, u.PhoneNumber, u.IsActive, u.CreatedAt));
    }

    [HttpPatch("{id}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, ChangeRoleRequest req)
    {
        var validRoles = new[] { "Admin", "StoreManager", "DeliveryDriver", "Customer" };
        if (!validRoles.Contains(req.Role)) return BadRequest(new { error = "Invalid role" });
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        u.Role = req.Role;
        await db.SaveChangesAsync();
        return Ok(new { id = u.Id, role = u.Role });
    }

    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        u.IsActive = !u.IsActive;
        await db.SaveChangesAsync();
        return Ok(new { id = u.Id, isActive = u.IsActive });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        db.Users.Remove(u);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
