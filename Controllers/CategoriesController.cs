using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.PRODUCT_CATEGORies.AsNoTracking().OrderBy(c => c.CATEGORY_ID).ToListAsync();
        return Ok(ApiResponse<IEnumerable<PRODUCT_CATEGORY>>.Ok(items));
    }
}