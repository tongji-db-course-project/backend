using backend.Data;
using backend.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 查询商品列表（分页 + 关键词 + 状态过滤）
    /// </summary>
    /// <param name="page">页码，从 1 开始</param>
    /// <param name="size">每页数量</param>
    /// <param name="keyword">按商品名称或条码模糊匹配</param>
    /// <param name="status">按商品状态精确过滤，如「在售」「停售」</param>
    [HttpGet(Name = "listProducts")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<ProductListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListProducts(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null)
    {
        // 防御性校验：页码 / 每页数量最小为 1，并限制单页上限避免一次拉取过多
        if (page < 1) page = 1;
        if (size < 1) size = 10;
        if (size > 100) size = 100;

        // 基础查询：关联类别与供应商
        var query = _db.PRODUCTs
            .AsNoTracking()
            .Include(p => p.CATEGORY)
            .Include(p => p.SUPPLIER)
            .AsQueryable();

        // 关键词：商品名称或条码模糊匹配
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(p =>
                p.PRODUCT_NAME.Contains(kw) ||
                (p.BARCODE != null && p.BARCODE.Contains(kw)));
        }

        // 状态精确过滤
        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim();
            query = query.Where(p => p.STATUS == st);
        }

        // 先取总数，再按主键排序分页
        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.PRODUCT_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new ProductListItemDto
            {
                productId = p.PRODUCT_ID,
                productName = p.PRODUCT_NAME,
                barcode = p.BARCODE,
                specification = p.SPECIFICATION,
                purchasePrice = p.PURCHASE_PRICE,
                salePrice = p.SALE_PRICE,
                stockWarning = p.STOCK_WARNING,
                unit = p.UNIT,
                status = p.STATUS,
                categoryId = p.CATEGORY_ID,
                categoryName = p.CATEGORY.CATEGORY_NAME,
                supplierId = p.SUPPLIER_ID,
                supplierName = p.SUPPLIER.SUPPLIER_NAME,
                // 一个商品对应一条库存记录，取其当前库存；无记录时为 0
                currentStock = p.INVENTORies
                    .Select(i => (int?)i.CURRENT_STOCK)
                    .FirstOrDefault() ?? 0
            })
            .ToListAsync();

        var result = new PageResult<ProductListItemDto>
        {
            list = items,
            total = total,
            page = page,
            size = size
        };

        return Ok(ApiResponse<PageResult<ProductListItemDto>>.Ok(result));
    }
}
