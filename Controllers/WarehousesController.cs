using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Services;
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
    [Obsolete("单仓库模式下仓库列表接口仅用于兼容旧客户端")]
    public async Task<IActionResult> List()
    {
        var warehouseId = await SystemWarehouse.GetIdAsync(_db);
        var items = await _db.WAREHOUSEs.AsNoTracking()
            .Where(w => w.WAREHOUSE_ID == warehouseId)
            .ToListAsync();
        return Ok(ApiResponse<IEnumerable<WAREHOUSE>>.Ok(items));
    }
}
