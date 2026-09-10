using System.Data;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class StockCheckService : IStockCheckService
{
    private readonly AppDbContext _db;
    public StockCheckService(AppDbContext db) => _db = db;

    public async Task<PageResult<StockCheckListItemDto>> ListAsync(int page, int size, string? status)
    {
        page = Math.Max(1, page); size = Math.Clamp(size, 1, 100);
        var query = _db.STOCK_CHECK_ORDERs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.STATUS == status.Trim());
        var total = await query.CountAsync();
        var list = await query.OrderByDescending(x => x.CHECK_DATE).ThenByDescending(x => x.CHECK_ID)
            .Skip((page - 1) * size).Take(size).Select(x => new StockCheckListItemDto
            {
                checkId = x.CHECK_ID, checkNo = x.CHECK_NO,
                productName = x.STOCK_CHECK_DETAILs.Select(d => d.PRODUCT.PRODUCT_NAME).FirstOrDefault() ?? "",
                status = x.STATUS ?? "", operatorName = x.OPERATOR.REAL_NAME ?? x.OPERATOR.USERNAME,
                checkDate = x.CHECK_DATE, completeDate = x.COMPLETE_DATE, remark = x.REMARK
            }).ToListAsync();
        return new PageResult<StockCheckListItemDto> { list = list, total = total, page = page, size = size };
    }

    public async Task<StockCheckDetailDto> GetAsync(int checkId)
    {
        if (checkId <= 0) throw new BusinessException(400, "盘点单编号必须大于0");
        return await DetailQuery().SingleOrDefaultAsync(x => x.checkId == checkId)
            ?? throw new BusinessException(404, "盘点单不存在");
    }

    public async Task<StockCheckDetailDto> CreateAsync(CreateStockCheckRequest request, int operatorId)
    {
        if (request.productId <= 0) throw new BusinessException(400, "请选择有效商品");
        var productIds = new List<int> { request.productId };
        if (!await _db.SYS_USERs.AsNoTracking().AnyAsync(x => x.USER_ID == operatorId && x.STATUS == "启用"))
            throw new BusinessException(401, "登录用户不存在或已禁用");
        var warehouseId = await SystemWarehouse.GetIdAsync(_db);
        var products = await _db.PRODUCTs.Where(x => productIds.Contains(x.PRODUCT_ID)).OrderBy(x => x.PRODUCT_ID).ToListAsync();
        if (products.Count != productIds.Count) throw new BusinessException(404, "部分盘点商品不存在");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var inventories = await _db.INVENTORies.Where(x => x.WAREHOUSE_ID == warehouseId && productIds.Contains(x.PRODUCT_ID)).ToListAsync();
        foreach (var productId in productIds.Except(inventories.Select(x => x.PRODUCT_ID)))
            _db.INVENTORies.Add(new INVENTORY { PRODUCT_ID = productId, WAREHOUSE_ID = warehouseId, CURRENT_STOCK = 0, LAST_UPDATE_TIME = DateTime.Now, IS_LOCKED = "否" });
        await _db.SaveChangesAsync();

        var inList = string.Join(",", productIds);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM INVENTORY WHERE WAREHOUSE_ID = {0} AND PRODUCT_ID IN (" + inList + ") ORDER BY PRODUCT_ID FOR UPDATE", warehouseId);
        foreach (var inventory in inventories) await _db.Entry(inventory).ReloadAsync();
        inventories = await _db.INVENTORies.Where(x => x.WAREHOUSE_ID == warehouseId && productIds.Contains(x.PRODUCT_ID)).OrderBy(x => x.PRODUCT_ID).ToListAsync();
        var locked = inventories.FirstOrDefault(x => x.IS_LOCKED == "是");
        if (locked is not null) throw new BusinessException(409, $"商品 {locked.PRODUCT_ID} 正在盘点，盘点单号：{locked.LOCK_NO}");

        var now = DateTime.Now;
        var order = new STOCK_CHECK_ORDER
        {
            CHECK_NO = $"PD{now:yyyyMMddHHmmssfff}{Guid.NewGuid():N}"[..30], WAREHOUSE_ID = warehouseId,
            STATUS = "盘点中", OPERATOR_ID = operatorId,
            CHECK_DATE = now, REMARK = request.remark?.Trim()
        };
        foreach (var inventory in inventories)
        {
            inventory.IS_LOCKED = "是"; inventory.LOCK_NO = order.CHECK_NO; inventory.LOCK_TIME = now;
            order.STOCK_CHECK_DETAILs.Add(new STOCK_CHECK_DETAIL
            {
                PRODUCT_ID = inventory.PRODUCT_ID, SYSTEM_QTY = inventory.CURRENT_STOCK,
                ADJUST_PRICE = products.First(x => x.PRODUCT_ID == inventory.PRODUCT_ID).PURCHASE_PRICE
            });
        }
        _db.STOCK_CHECK_ORDERs.Add(order);
        await _db.SaveChangesAsync(); await transaction.CommitAsync();
        return await GetAsync(order.CHECK_ID);
    }

    public async Task<StockCheckDetailDto> ConfirmAsync(int checkId, ConfirmStockCheckRequest request, int operatorId)
    {
        if (request.items.Select(x => x.productId).Distinct().Count() != request.items.Count)
            throw new BusinessException(400, "实盘商品不能重复");
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM STOCK_CHECK_ORDER WHERE CHECK_ID = {0} FOR UPDATE", checkId);
        var order = await _db.STOCK_CHECK_ORDERs.Include(x => x.STOCK_CHECK_DETAILs).SingleOrDefaultAsync(x => x.CHECK_ID == checkId)
            ?? throw new BusinessException(404, "盘点单不存在");
        if (order.STATUS != "盘点中") throw new BusinessException(409, "只有盘点中的单据可以确认");
        var actuals = request.items.ToDictionary(x => x.productId, x => x.actualQty!.Value);
        if (actuals.Count != order.STOCK_CHECK_DETAILs.Count || order.STOCK_CHECK_DETAILs.Any(x => !actuals.ContainsKey(x.PRODUCT_ID)))
            throw new BusinessException(400, "必须录入盘点单内全部商品的实际数量");
        var productIds = actuals.Keys.OrderBy(x => x).ToList();
        var inList = string.Join(",", productIds);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM INVENTORY WHERE WAREHOUSE_ID = {0} AND PRODUCT_ID IN (" + inList + ") ORDER BY PRODUCT_ID FOR UPDATE", order.WAREHOUSE_ID);
        var inventories = await _db.INVENTORies.Where(x => x.WAREHOUSE_ID == order.WAREHOUSE_ID && productIds.Contains(x.PRODUCT_ID)).ToListAsync();
        if (inventories.Count != productIds.Count || inventories.Any(x => x.IS_LOCKED != "是" || x.LOCK_NO != order.CHECK_NO))
            throw new BusinessException(409, "盘点商品锁定状态异常");
        var now = DateTime.Now;
        foreach (var detail in order.STOCK_CHECK_DETAILs)
        {
            var actual = actuals[detail.PRODUCT_ID]; var difference = actual - detail.SYSTEM_QTY;
            detail.ACTUAL_QTY = actual; detail.DIFFERENCE_QTY = difference; detail.ADJUST_AMOUNT = difference * (detail.ADJUST_PRICE ?? 0);
            var inventory = inventories.Single(x => x.PRODUCT_ID == detail.PRODUCT_ID);
            inventory.CURRENT_STOCK = actual; inventory.LAST_UPDATE_TIME = now;
            inventory.IS_LOCKED = "否"; inventory.LOCK_NO = null; inventory.LOCK_TIME = null;
            if (difference != 0) _db.INVENTORY_RECORDs.Add(new INVENTORY_RECORD
            {
                PRODUCT_ID = detail.PRODUCT_ID, RECORD_TYPE = difference > 0 ? "盘盈" : "盘亏",
                SOURCE_NO = order.CHECK_NO, CHANGE_QTY = difference, REMAIN_QTY = actual,
                OPERATOR_ID = operatorId, RECORD_TIME = now, REMARK = request.remark?.Trim() ?? "库存盘点调整"
            });
        }
        order.STATUS = "已完成"; order.COMPLETE_DATE = now;
        if (!string.IsNullOrWhiteSpace(request.remark)) order.REMARK = request.remark.Trim();
        await _db.SaveChangesAsync(); await transaction.CommitAsync();
        return await GetAsync(checkId);
    }

    public async Task CancelAsync(int checkId, int operatorId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM STOCK_CHECK_ORDER WHERE CHECK_ID = {0} FOR UPDATE", checkId);
        var order = await _db.STOCK_CHECK_ORDERs.Include(x => x.STOCK_CHECK_DETAILs).SingleOrDefaultAsync(x => x.CHECK_ID == checkId)
            ?? throw new BusinessException(404, "盘点单不存在");
        if (order.STATUS != "盘点中") throw new BusinessException(409, "只有盘点中的单据可以作废");
        var ids = order.STOCK_CHECK_DETAILs.Select(x => x.PRODUCT_ID).OrderBy(x => x).ToList();
        var inList = string.Join(",", ids);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM INVENTORY WHERE WAREHOUSE_ID = {0} AND PRODUCT_ID IN (" + inList + ") ORDER BY PRODUCT_ID FOR UPDATE", order.WAREHOUSE_ID);
        var inventories = await _db.INVENTORies.Where(x => x.WAREHOUSE_ID == order.WAREHOUSE_ID && ids.Contains(x.PRODUCT_ID)).ToListAsync();
        foreach (var inventory in inventories.Where(x => x.LOCK_NO == order.CHECK_NO))
        { inventory.IS_LOCKED = "否"; inventory.LOCK_NO = null; inventory.LOCK_TIME = null; }
        order.STATUS = "已作废"; order.COMPLETE_DATE = DateTime.Now;
        await _db.SaveChangesAsync(); await transaction.CommitAsync();
    }

    private IQueryable<StockCheckDetailDto> DetailQuery() => _db.STOCK_CHECK_ORDERs.AsNoTracking().Select(x => new StockCheckDetailDto
    {
        checkId = x.CHECK_ID, checkNo = x.CHECK_NO,
        productName = x.STOCK_CHECK_DETAILs.Select(d => d.PRODUCT.PRODUCT_NAME).FirstOrDefault() ?? "",
        status = x.STATUS ?? "",
        operatorName = x.OPERATOR.REAL_NAME ?? x.OPERATOR.USERNAME,
        checkDate = x.CHECK_DATE, completeDate = x.COMPLETE_DATE, remark = x.REMARK,
        items = x.STOCK_CHECK_DETAILs.OrderBy(d => d.PRODUCT_ID).Select(d => new StockCheckDetailItemDto
        {
            productId = d.PRODUCT_ID, productName = d.PRODUCT.PRODUCT_NAME, barcode = d.PRODUCT.BARCODE, unit = d.PRODUCT.UNIT,
            systemQty = d.SYSTEM_QTY, actualQty = d.ACTUAL_QTY, differenceQty = d.DIFFERENCE_QTY,
            adjustPrice = d.ADJUST_PRICE, adjustAmount = d.ADJUST_AMOUNT
        }).ToList()
    });
}
