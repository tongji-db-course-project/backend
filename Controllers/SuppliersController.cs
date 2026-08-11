using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("suppliers")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _db;

    public SuppliersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);

        var query = _db.SUPPLIERs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(s => s.SUPPLIER_NAME.Contains(kw) || (s.CONTACT_NAME != null && s.CONTACT_NAME.Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.STATUS == status.Trim());

        var total = await query.CountAsync();
        var items = await query.OrderBy(s => s.SUPPLIER_ID).Skip((page - 1) * size).Take(size).ToListAsync();

        return Ok(ApiResponse<PageResult<SUPPLIER>>.Ok(new PageResult<SUPPLIER> { list = items, total = total, page = page, size = size }));
    }

    [HttpGet("{supplierId:int}")]
    public async Task<IActionResult> GetById(int supplierId)
    {
        var item = await _db.SUPPLIERs.AsNoTracking().FirstOrDefaultAsync(s => s.SUPPLIER_ID == supplierId);
        if (item == null) return NotFound(ApiResponse<string>.Fail(404, "供应商不存在"));
        return Ok(ApiResponse<SUPPLIER>.Ok(item));
    }
}