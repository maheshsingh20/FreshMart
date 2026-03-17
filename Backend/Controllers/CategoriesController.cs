using Backend.Data;
using Backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cats = await db.Categories
            .Select(c => new CategoryDto(c.Id.ToString(), c.Name, c.Description, c.ImageUrl, c.ParentCategoryId.HasValue ? c.ParentCategoryId.ToString() : null))
            .ToListAsync();
        return Ok(cats);
    }
}
