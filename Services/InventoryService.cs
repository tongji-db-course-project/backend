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
        int page, int size, string? keyword, string? status, int? productId)
    {
        NormalizePage(ref page, ref size);
        var warehouseId = await GetDefaultWarehouseIdAsync();

        var query = BuildInventoryQuery(warehouseId, keyword, status, productId);
        var total = await query.CountAsync();
        var list = await query
            .OrderBy(x => x.PRODUCT_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => MapInventory(x))
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
            .Select(x => MapInventory(x))
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
            .Select(x => MapInventory(x))
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
        int page, int size, string? keyword, string? status)
    {
        NormalizePage(ref page, ref size);
        var warehouseId = await GetDefaultWarehouseIdAsync();

        var query = BuildInventoryQuery(warehouseId, keyword, status, null)
            .Where(x => x.PRODUCT.STOCK_WARNING != null &&
                        x.CURRENT_STOCK <= x.PRODUCT.STOCK_WARNING);

        var total = await query.CountAsync();
        var list = await query
            .OrderBy(x => x.CURRENT_STOCK)
            .ThenBy(x => x.PRODUCT_ID)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => MapInventory(x))
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

        var recordType = request.recordType.Trim();
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
                // 种子数据显式写入了 Identity 主键，数据库 Identity 的下一值可能未推进。
                // 单仓库课程项目中显式使用当前最大值 + 1，避免首次新增时主键冲突。
                INVENTORY_ID = await GetNextInventoryIdAsync(),
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
            // 原始种子流水使用 record_id 1..5，显式生成后续编号以避开 Identity 冲突。
            RECORD_ID = await GetNextInventoryRecordIdAsync(),
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

        return MapInventory(inventory);
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

    private async Task<int> GetNextInventoryIdAsync()
    {
        var maxId = await _db.INVENTORies
            .Select(x => (int?)x.INVENTORY_ID)
            .MaxAsync() ?? 0;
        return checked(maxId + 1);
    }

    private async Task<int> GetNextInventoryRecordIdAsync()
    {
        var maxId = await _db.INVENTORY_RECORDs
            .Select(x => (int?)x.RECORD_ID)
            .MaxAsync() ?? 0;
        return checked(maxId + 1);
    }

    private static InventoryDto MapInventory(INVENTORY inventory) => new()
    {
        inventoryId = inventory.INVENTORY_ID,
        productId = inventory.PRODUCT_ID,
        warehouseId = inventory.WAREHOUSE_ID,
        currentStock = inventory.CURRENT_STOCK,
        lastUpdateTime = inventory.LAST_UPDATE_TIME
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
