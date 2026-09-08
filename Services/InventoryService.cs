using System.Linq.Expressions;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

/// <summary>
/// 单仓库模式下的库存查询、预警、流水和手动调整。
/// </summary>
public class InventoryService : IInventoryService
{
    private static readonly HashSet<string> AllowedManualRecordTypes =
        new(StringComparer.Ordinal) { "手动入库", "手动出库", "盘点" };

    private readonly AppDbContext _db;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(AppDbContext db, ILogger<InventoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PageResult<InventoryDto>> ListInventoryAsync(
        int page, int size, string? keyword, string? status, int? productId, int? warehouseId)
    {
        NormalizePage(ref page, ref size);
        var resolvedWarehouseId = await ResolveWarehouseIdAsync(warehouseId);

        var query = BuildInventoryQuery(resolvedWarehouseId, keyword, status, productId);
        var total = await query.CountAsync();
        var list = await query
            .OrderBy(x => x.PRODUCT_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(MapInventoryExpr)
            .ToListAsync();

        return new PageResult<InventoryDto>
        {
            list = list,
            total = total,
            page = page,
            size = size
        };
    }

    public async Task<InventoryDto> GetInventoryAsync(int inventoryId)
    {
        if (inventoryId <= 0)
            throw new BusinessException(400, "库存编号必须大于0");

        var warehouseId = await GetDefaultWarehouseIdAsync();
        var result = await _db.INVENTORies
            .AsNoTracking()
            .Where(x => x.INVENTORY_ID == inventoryId && x.WAREHOUSE_ID == warehouseId)
            .Select(MapInventoryExpr)
            .SingleOrDefaultAsync();

        return result ?? throw new KeyNotFoundException("库存记录不存在");
    }

    public async Task<InventoryDto> GetInventoryByProductAsync(int productId)
    {
        if (productId <= 0)
            throw new BusinessException(400, "商品编号必须大于0");

        var warehouseId = await GetDefaultWarehouseIdAsync();
        var result = await _db.INVENTORies
            .AsNoTracking()
            .Where(x => x.PRODUCT_ID == productId && x.WAREHOUSE_ID == warehouseId)
            .Select(MapInventoryExpr)
            .SingleOrDefaultAsync();

        if (result is not null)
            return result;

        var productExists = await _db.PRODUCTs
            .AsNoTracking()
            .AnyAsync(x => x.PRODUCT_ID == productId);

        throw new KeyNotFoundException(
            productExists ? "该商品暂无库存记录" : "商品不存在");
    }

    public async Task<PageResult<InventoryDto>> ListWarningAsync(
        int page, int size, string? keyword, string? status, int? warehouseId)
    {
        NormalizePage(ref page, ref size);
        var resolvedWarehouseId = await ResolveWarehouseIdAsync(warehouseId);

        var query = BuildInventoryQuery(resolvedWarehouseId, keyword, status, null)
            .Where(x => x.PRODUCT.STOCK_WARNING != null &&
                        x.CURRENT_STOCK <= x.PRODUCT.STOCK_WARNING);

        var total = await query.CountAsync();
        var list = await query
            .OrderBy(x => x.CURRENT_STOCK)
            .ThenBy(x => x.PRODUCT_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(MapInventoryExpr)
            .ToListAsync();

        return new PageResult<InventoryDto>
        {
            list = list,
            total = total,
            page = page,
            size = size
        };
    }

    public async Task<PageResult<InventoryRecordDto>> ListRecordsAsync(
        int page, int size, string? keyword, int? productId, string? recordType)
    {
        NormalizePage(ref page, ref size);

        var query = _db.INVENTORY_RECORDs.AsNoTracking().AsQueryable();

        if (productId.HasValue)
        {
            if (productId.Value <= 0)
                throw new BusinessException(400, "商品编号必须大于0");

            query = query.Where(x => x.PRODUCT_ID == productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x =>
                x.PRODUCT.PRODUCT_NAME.Contains(value) ||
                (x.PRODUCT.BARCODE != null && x.PRODUCT.BARCODE.Contains(value)) ||
                (x.SOURCE_NO != null && x.SOURCE_NO.Contains(value)));
        }

        if (!string.IsNullOrWhiteSpace(recordType))
        {
            var value = recordType.Trim();
            query = query.Where(x => x.RECORD_TYPE == value);
        }

        var total = await query.CountAsync();
        var list = await query
            .OrderByDescending(x => x.RECORD_TIME)
            .ThenByDescending(x => x.RECORD_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new InventoryRecordDto
            {
                recordId = x.RECORD_ID,
                productId = x.PRODUCT_ID,
                recordType = x.RECORD_TYPE,
                sourceNo = x.SOURCE_NO,
                changeQty = x.CHANGE_QTY,
                remainQty = x.REMAIN_QTY,
                operatorId = x.OPERATOR_ID,
                recordTime = x.RECORD_TIME,
                remark = x.REMARK
            })
            .ToListAsync();

        return new PageResult<InventoryRecordDto>
        {
            list = list,
            total = total,
            page = page,
            size = size
        };
    }

    public async Task<InventoryDto> AdjustInventoryAsync(
        InventoryAdjustDto request, int operatorId)
    {
        if (request.changeQty == 0)
            throw new BusinessException(400, "库存变动数量不能为0");

        var recordType = request.recordType.Trim() == "盘点调整" ? "盘点" : request.recordType.Trim();
        if (!AllowedManualRecordTypes.Contains(recordType))
            throw new BusinessException(400, "流水类型仅允许：手动入库、手动出库、盘点");

        var warehouseId = await GetDefaultWarehouseIdAsync();
        var productExists = await _db.PRODUCTs
            .AsNoTracking()
            .AnyAsync(x => x.PRODUCT_ID == request.productId);
        if (!productExists)
            throw new KeyNotFoundException("商品不存在");

        var operatorExists = await _db.SYS_USERs
            .AsNoTracking()
            .AnyAsync(x => x.USER_ID == operatorId && x.STATUS == "启用");
        if (!operatorExists)
            throw new BusinessException(401, "登录用户不存在或已被禁用");

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var inventory = await _db.INVENTORies
            .SingleOrDefaultAsync(x =>
                x.PRODUCT_ID == request.productId && x.WAREHOUSE_ID == warehouseId);

        if (inventory is null)
        {
            if (request.changeQty < 0)
                throw new BusinessException(409, "库存不足，该商品暂无库存记录");

            inventory = new INVENTORY
            {
                PRODUCT_ID = request.productId,
                WAREHOUSE_ID = warehouseId,
                CURRENT_STOCK = request.changeQty,
                LAST_UPDATE_TIME = DateTime.Now
            };
            _db.INVENTORies.Add(inventory);
        }
        else
        {
            var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE INVENTORY
                   SET CURRENT_STOCK = CURRENT_STOCK + {request.changeQty},
                       LAST_UPDATE_TIME = SYSDATE
                 WHERE INVENTORY_ID = {inventory.INVENTORY_ID}
                   AND CURRENT_STOCK + {request.changeQty} >= 0");

            if (affectedRows == 0)
                throw new BusinessException(409, $"库存不足，当前库存为{inventory.CURRENT_STOCK}");

            await _db.Entry(inventory).ReloadAsync();
        }

        _db.INVENTORY_RECORDs.Add(new INVENTORY_RECORD
        {
            PRODUCT_ID = request.productId,
            RECORD_TYPE = recordType,
            SOURCE_NO = NormalizeOptional(request.sourceNo),
            CHANGE_QTY = request.changeQty,
            REMAIN_QTY = inventory.CURRENT_STOCK,
            OPERATOR_ID = operatorId,
            RECORD_TIME = DateTime.Now,
            REMARK = NormalizeOptional(request.remark)
        });

        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "库存调整写入失败，商品 {ProductId}，操作人 {OperatorId}",
                request.productId, operatorId);
            throw new BusinessException(409, "库存调整失败，请刷新后重试");
        }

        return await GetInventoryByProductAsync(request.productId);
    }

    public async Task<IReadOnlyList<SupplierPurchaseSuggestionDto>> GetPurchaseSuggestionsAsync()
    {
        var warehouseId = await GetDefaultWarehouseIdAsync();
        var warnings = await _db.PRODUCTs.AsNoTracking()
            .Where(x => x.STATUS == "在售")
            .Select(x => new
            {
                x.PRODUCT_ID,
                x.PRODUCT_NAME,
                x.STOCK_WARNING,
                x.SUPPLIER_ID,
                x.SUPPLIER.SUPPLIER_NAME,
                x.SUPPLIER.MIN_ORDER_QTY,
                CurrentStock = x.INVENTORies.Where(i => i.WAREHOUSE_ID == warehouseId)
                    .Select(i => (int?)i.CURRENT_STOCK).FirstOrDefault() ?? 0
            })
            .Where(x => x.STOCK_WARNING.HasValue && x.CurrentStock <= x.STOCK_WARNING.Value)
            .ToListAsync();

        return warnings.GroupBy(x => new { x.SUPPLIER_ID, x.SUPPLIER_NAME })
            .Select(group => new SupplierPurchaseSuggestionDto
            {
                supplierId = group.Key.SUPPLIER_ID,
                supplierName = group.Key.SUPPLIER_NAME,
                items = group.Select(x => new PurchaseSuggestionItemDto
                {
                    productId = x.PRODUCT_ID,
                    productName = x.PRODUCT_NAME,
                    currentStock = x.CurrentStock,
                    stockWarning = x.STOCK_WARNING ?? 0,
                    suggestedQuantity = Math.Max(x.MIN_ORDER_QTY ?? 0, Math.Max(1, (x.STOCK_WARNING ?? 0) * 2 - x.CurrentStock))
                }).OrderBy(x => x.productId).ToList()
            }).OrderBy(x => x.supplierId).ToList();
    }

    private IQueryable<INVENTORY> BuildInventoryQuery(
        int warehouseId, string? keyword, string? status, int? productId)
    {
        var query = _db.INVENTORies
            .AsNoTracking()
            .Where(x => x.WAREHOUSE_ID == warehouseId);

        if (productId.HasValue)
        {
            if (productId.Value <= 0)
                throw new BusinessException(400, "商品编号必须大于0");

            query = query.Where(x => x.PRODUCT_ID == productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x =>
                x.PRODUCT.PRODUCT_NAME.Contains(value) ||
                (x.PRODUCT.BARCODE != null && x.PRODUCT.BARCODE.Contains(value)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var value = status.Trim();
            query = query.Where(x => x.PRODUCT.STATUS == value);
        }

        return query;
    }

    private async Task<int> GetDefaultWarehouseIdAsync()
    {
        var warehouseIds = await _db.WAREHOUSEs
            .AsNoTracking()
            .Where(x => x.STATUS == "启用")
            .OrderBy(x => x.WAREHOUSE_ID)
            .Select(x => x.WAREHOUSE_ID)
            .Take(2)
            .ToListAsync();

        return warehouseIds.Count switch
        {
            0 => throw new BusinessException(409, "系统未配置启用仓库"),
            > 1 => throw new BusinessException(409, "单仓库模式下只能配置一个启用仓库"),
            _ => warehouseIds[0]
        };
    }

    private async Task<int> ResolveWarehouseIdAsync(int? warehouseId)
    {
        if (!warehouseId.HasValue) return await GetDefaultWarehouseIdAsync();
        if (warehouseId.Value <= 0) throw new BusinessException(400, "仓库编号必须大于0");
        if (!await _db.WAREHOUSEs.AsNoTracking().AnyAsync(x => x.WAREHOUSE_ID == warehouseId.Value))
            throw new KeyNotFoundException("仓库不存在");
        return warehouseId.Value;
    }

    /// <summary>
    /// 库存映射表达式：JOIN 商品/仓库表展平名称、条码、预警值等字段，
    /// 供 SELECT 在数据库端执行（前端库存页依赖 stockWarning 判断预警状态）
    /// </summary>
    private static readonly Expression<Func<INVENTORY, InventoryDto>> MapInventoryExpr =
        x => new InventoryDto
        {
            inventoryId = x.INVENTORY_ID,
            productId = x.PRODUCT_ID,
            productName = x.PRODUCT.PRODUCT_NAME,
            barcode = x.PRODUCT.BARCODE,
            specification = x.PRODUCT.SPECIFICATION,
            unit = x.PRODUCT.UNIT,
            stockWarning = x.PRODUCT.STOCK_WARNING,
            warehouseId = x.WAREHOUSE_ID,
            warehouseName = x.WAREHOUSE.WAREHOUSE_NAME,
            currentStock = x.CURRENT_STOCK,
            lastUpdateTime = x.LAST_UPDATE_TIME
        };

    private static void NormalizePage(ref int page, ref int size)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 10;
        if (size > 100) size = 100;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (value is null)
            return null;

        var text = value.Trim();
        return text.Length == 0 ? null : text;
    }
}
