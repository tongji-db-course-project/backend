using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("warehouses")]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly AppDbContext _db;

    public WarehousesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.WAREHOUSEs.AsNoTracking().OrderBy(w => w.WAREHOUSE_ID).ToListAsync();
        return Ok(ApiResponse<IEnumerable<WAREHOUSE>>.Ok(items));
    }
}