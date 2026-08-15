using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _db;

    public InventoryController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] int? productId = null,
        [FromQuery] int? warehouseId = null)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);

        var query = _db.INVENTORies.AsNoTracking().AsQueryable();

        if (productId.HasValue) query = query.Where(i => i.PRODUCT_ID == productId.Value);
        if (warehouseId.HasValue) query = query.Where(i => i.WAREHOUSE_ID == warehouseId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderBy(i => i.INVENTORY_ID).Skip((page - 1) * size).Take(size)
            .Select(i => new
            {
                i.INVENTORY_ID,
                i.PRODUCT_ID,
                productName = i.PRODUCT.PRODUCT_NAME,
                i.WAREHOUSE_ID,
                warehouseName = i.WAREHOUSE.WAREHOUSE_NAME,
                i.CURRENT_STOCK,
                i.LAST_UPDATE_TIME
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new { list = items, total, page, size }));
    }

    [HttpGet("records")]
    public async Task<IActionResult> GetRecords(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] int? productId = null)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);

        var query = _db.INVENTORY_RECORDs.AsNoTracking().AsQueryable();

        if (productId.HasValue) query = query.Where(r => r.PRODUCT_ID == productId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(r => r.RECORD_TIME).Skip((page - 1) * size).Take(size)
            .Select(r => new
            {
                r.RECORD_ID,
                r.PRODUCT_ID,
                productName = r.PRODUCT.PRODUCT_NAME,
                r.RECORD_TYPE,
                r.SOURCE_NO,
                r.CHANGE_QTY,
                r.REMAIN_QTY,
                r.RECORD_TIME,
                r.REMARK
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new { list = items, total, page, size }));
    }
}