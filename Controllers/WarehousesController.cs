using backend.Data;
using backend.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

/// <summary>
/// 仓库管理
/// </summary>
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

    /// <summary>
    /// 查询仓库列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.WAREHOUSEs
            .AsNoTracking()
            .OrderBy(w => w.WAREHOUSE_ID)
            .Select(w => new WarehouseDto
            {
                warehouseId = w.WAREHOUSE_ID,
                warehouseName = w.WAREHOUSE_NAME,
                address = w.ADDRESS,
                status = w.STATUS,
                createTime = w.CREATE_TIME
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<WarehouseDto>>.Ok(items));
    }
}
