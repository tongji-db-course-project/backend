using System.Data;
using System.Text;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class PurchaseReturnService : IPurchaseReturnService
{
    private const string Pending = "待审核";
    private const string Approved = "已审核";
    private const string Completed = "已完成";
    private const string Voided = "已作废";
    private const string LogType = "采购退货单";
    private readonly AppDbContext _db;

    public PurchaseReturnService(AppDbContext db) => _db = db;

    public async Task<PageResult<PurchaseReturnDto>> ListAsync(int page, int size, string? keyword, string? status,
        int? supplierId, int? purchaseId, DateTime? startDate, DateTime? endDate)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);
        if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
            throw new ArgumentException("开始日期不能晚于结束日期");

        var query = _db.PURCHASE_RETURN_ORDERs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.RETURN_NO.Contains(value) || x.PURCHASE.ORDER_CODE.Contains(value) ||
                x.SUPPLIER.SUPPLIER_NAME.Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.STATUS == status.Trim());
        if (supplierId.HasValue) query = query.Where(x => x.SUPPLIER_ID == supplierId.Value);
        if (purchaseId.HasValue) query = query.Where(x => x.PURCHASE_ID == purchaseId.Value);
        if (startDate.HasValue) query = query.Where(x => x.RETURN_DATE >= startDate.Value.Date);
        if (endDate.HasValue) query = query.Where(x => x.RETURN_DATE < endDate.Value.Date.AddDays(1));

        var total = await query.CountAsync();
        var list = await Project(query.OrderByDescending(x => x.RETURN_DATE).ThenByDescending(x => x.RETURN_ID), false)
            .Skip((page - 1) * size).Take(size).ToListAsync();
        return new PageResult<PurchaseReturnDto> { list = list, total = total, page = page, size = size };
    }

    public async Task<PurchaseReturnDto> GetAsync(int returnId) =>
        await Project(_db.PURCHASE_RETURN_ORDERs.AsNoTracking().Where(x => x.RETURN_ID == returnId), true)
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException("采购退货单不存在");

    public async Task<PurchaseReturnDto> CreateAsync(SavePurchaseReturnRequest request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM PURCHASE_ORDER WHERE ORDER_ID = {0} FOR UPDATE", request.purchaseId);
        var validated = await ValidateRequestAsync(request, null);
        var now = DateTime.Now;
        var order = new PURCHASE_RETURN_ORDER
        {
            RETURN_NO = $"PR{now:yyyyMMddHHmmssfff}{Guid.NewGuid():N}"[..30],
            PURCHASE_ID = validated.purchase.ORDER_ID,
            SUPPLIER_ID = validated.purchase.SUPPLIER_ID!.Value,
            OPERATOR_ID = request.operatorId,
            RETURN_DATE = request.returnDate ?? now,
            TOTAL_AMOUNT = validated.details.Sum(x => x.subtotal),
            STATUS = Pending,
            CREATE_TIME = now,
            UPDATE_TIME = now,
            REMARK = request.remark?.Trim()
        };
        foreach (var detail in validated.details)
        {
            order.PURCHASE_RETURN_ORDER_DETAILs.Add(new PURCHASE_RETURN_ORDER_DETAIL
            {
                PRODUCT_ID = detail.productId,
                QUANTITY = detail.quantity,
                RETURN_PRICE = detail.returnPrice,
                SUBTOTAL = detail.subtotal
            });
        }
        _db.PURCHASE_RETURN_ORDERs.Add(order);
        await _db.SaveChangesAsync();
        AddLog(order.RETURN_ID, null, Pending, request.operatorId, request.remark);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(order.RETURN_ID);
    }

    public async Task<PurchaseReturnDto> UpdateAsync(int returnId, SavePurchaseReturnRequest request)
    {
        var existing = await _db.PURCHASE_RETURN_ORDERs.AsNoTracking()
            .Where(x => x.RETURN_ID == returnId).Select(x => new { x.PURCHASE_ID }).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("采购退货单不存在");
        if (existing.PURCHASE_ID != request.purchaseId) throw new ArgumentException("不能修改采购退货单关联的原采购单");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM PURCHASE_ORDER WHERE ORDER_ID = {0} FOR UPDATE", request.purchaseId);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM PURCHASE_RETURN_ORDER WHERE RETURN_ID = {0} FOR UPDATE", returnId);
        var order = await _db.PURCHASE_RETURN_ORDERs.Include(x => x.PURCHASE_RETURN_ORDER_DETAILs)
            .FirstAsync(x => x.RETURN_ID == returnId);
        if (order.STATUS != Pending) throw new InvalidOperationException("仅待审核采购退货单可以修改");
        var validated = await ValidateRequestAsync(request, returnId);

        order.OPERATOR_ID = request.operatorId;
        order.RETURN_DATE = request.returnDate ?? order.RETURN_DATE;
        order.TOTAL_AMOUNT = validated.details.Sum(x => x.subtotal);
        order.UPDATE_TIME = DateTime.Now;
        order.REMARK = request.remark?.Trim();
        _db.PURCHASE_RETURN_ORDER_DETAILs.RemoveRange(order.PURCHASE_RETURN_ORDER_DETAILs);
        foreach (var detail in validated.details)
        {
            order.PURCHASE_RETURN_ORDER_DETAILs.Add(new PURCHASE_RETURN_ORDER_DETAIL
            {
                PRODUCT_ID = detail.productId,
                QUANTITY = detail.quantity,
                RETURN_PRICE = detail.returnPrice,
                SUBTOTAL = detail.subtotal
            });
        }
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(returnId);
    }

    public async Task<PurchaseReturnDto> ApproveAsync(int returnId, ApprovalRequest request)
    {
        if (!await _db.SYS_USERs.AsNoTracking().AnyAsync(x => x.USER_ID == request.approverId && x.STATUS == "启用"))
            throw new KeyNotFoundException("审核人不存在或已禁用");
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM PURCHASE_RETURN_ORDER WHERE RETURN_ID = {0} FOR UPDATE", returnId);
        var order = await _db.PURCHASE_RETURN_ORDERs.FirstOrDefaultAsync(x => x.RETURN_ID == returnId)
            ?? throw new KeyNotFoundException("采购退货单不存在");
        if (order.STATUS != Pending) throw new InvalidOperationException("仅待审核采购退货单可以审核");
        order.STATUS = Approved;
        order.UPDATE_TIME = DateTime.Now;
        AddLog(returnId, Pending, Approved, request.approverId, request.remark);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(returnId);
    }

    public async Task<PurchaseReturnDto> CompleteAsync(int returnId, CompletePurchaseReturnRequest request)
    {
        if (!await _db.SYS_USERs.AsNoTracking().AnyAsync(x => x.USER_ID == request.operatorId && x.STATUS == "启用"))
            throw new KeyNotFoundException("经办人不存在或已禁用");
        if (!await _db.WAREHOUSEs.AsNoTracking().AnyAsync(x => x.WAREHOUSE_ID == request.warehouseId))
            throw new KeyNotFoundException("仓库不存在");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM PURCHASE_RETURN_ORDER WHERE RETURN_ID = {0} FOR UPDATE", returnId);
        var order = await _db.PURCHASE_RETURN_ORDERs.Include(x => x.PURCHASE_RETURN_ORDER_DETAILs)
            .FirstOrDefaultAsync(x => x.RETURN_ID == returnId) ?? throw new KeyNotFoundException("采购退货单不存在");
        if (order.STATUS != Approved) throw new InvalidOperationException("仅已审核采购退货单可以完成退货");

        var productIds = order.PURCHASE_RETURN_ORDER_DETAILs.Select(x => x.PRODUCT_ID).OrderBy(x => x).ToList();
        var inList = string.Join(",", productIds);
        await _db.Database.ExecuteSqlRawAsync(
            "SELECT * FROM INVENTORY WHERE WAREHOUSE_ID = {0} AND PRODUCT_ID IN (" + inList + ") ORDER BY PRODUCT_ID FOR UPDATE",
            request.warehouseId);
        var inventories = await _db.INVENTORies.Where(x => x.WAREHOUSE_ID == request.warehouseId && productIds.Contains(x.PRODUCT_ID))
            .ToListAsync();
        if (inventories.Count != productIds.Count) throw new InvalidOperationException("部分退货商品在指定仓库没有库存记录");
        foreach (var detail in order.PURCHASE_RETURN_ORDER_DETAILs)
        {
            var inventory = inventories.First(x => x.PRODUCT_ID == detail.PRODUCT_ID);
            if (inventory.CURRENT_STOCK < detail.QUANTITY)
                throw new InvalidOperationException($"商品 {detail.PRODUCT_ID} 库存不足，无法采购退货");
        }

        var now = DateTime.Now;
        foreach (var detail in order.PURCHASE_RETURN_ORDER_DETAILs)
        {
            var inventory = inventories.First(x => x.PRODUCT_ID == detail.PRODUCT_ID);
            inventory.CURRENT_STOCK -= detail.QUANTITY;
            inventory.LAST_UPDATE_TIME = now;
            _db.INVENTORY_RECORDs.Add(new INVENTORY_RECORD
            {
                PRODUCT_ID = detail.PRODUCT_ID,
                RECORD_TYPE = "采购退货",
                SOURCE_NO = order.RETURN_NO,
                CHANGE_QTY = -detail.QUANTITY,
                REMAIN_QTY = inventory.CURRENT_STOCK,
                OPERATOR_ID = request.operatorId,
                RECORD_TIME = now,
                REMARK = request.remark?.Trim() ?? "采购退货出库"
            });
        }

        var settlement = await _db.SUPPLIER_SETTLEMENTs.FirstOrDefaultAsync(x => x.PURCHASE_ID == order.PURCHASE_ID);
        if (settlement is not null)
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT * FROM SUPPLIER_SETTLEMENT WHERE SETTLEMENT_ID = {0} FOR UPDATE", settlement.SETTLEMENT_ID);
            await _db.Entry(settlement).ReloadAsync();
            settlement.SETTLEMENT_AMOUNT = Math.Max(0, settlement.SETTLEMENT_AMOUNT - order.TOTAL_AMOUNT);
            settlement.UNPAID_AMOUNT = Math.Max(0, settlement.SETTLEMENT_AMOUNT - (settlement.PAID_AMOUNT ?? 0));
            settlement.STATUS = (settlement.PAID_AMOUNT ?? 0) <= 0 ? "未结算" :
                (settlement.PAID_AMOUNT ?? 0) >= settlement.SETTLEMENT_AMOUNT ? "已结算" : "部分结算";
            var note = $"退货 {order.RETURN_NO} 冲减 {order.TOTAL_AMOUNT:F2} 元";
            settlement.REMARK = string.IsNullOrWhiteSpace(settlement.REMARK)
                ? note
                : $"{settlement.REMARK}; {note}";
            // Oracle VARCHAR2(200) 按字节计（AL32UTF8 中文3字节/字），按 UTF8 字节安全截断
            settlement.REMARK = TruncateUtf8(settlement.REMARK, 200);
        }

        order.STATUS = Completed;
        order.OPERATOR_ID = request.operatorId;
        order.UPDATE_TIME = now;
        AddLog(returnId, Approved, Completed, request.operatorId, request.remark ?? "完成采购退货出库并冲减应付");
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(returnId);
    }

    public async Task CancelAsync(int returnId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM PURCHASE_RETURN_ORDER WHERE RETURN_ID = {0} FOR UPDATE", returnId);
        var order = await _db.PURCHASE_RETURN_ORDERs.FirstOrDefaultAsync(x => x.RETURN_ID == returnId)
            ?? throw new KeyNotFoundException("采购退货单不存在");
        if (order.STATUS == Completed) throw new InvalidOperationException("已完成采购退货单不能作废");
        if (order.STATUS == Voided) throw new InvalidOperationException("采购退货单已作废");
        var oldStatus = order.STATUS;
        order.STATUS = Voided;
        order.UPDATE_TIME = DateTime.Now;
        AddLog(returnId, oldStatus, Voided, order.OPERATOR_ID, "作废采购退货单");
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<IReadOnlyList<OrderStatusLogDto>> GetTimelineAsync(int returnId)
    {
        if (!await _db.PURCHASE_RETURN_ORDERs.AsNoTracking().AnyAsync(x => x.RETURN_ID == returnId))
            throw new KeyNotFoundException("采购退货单不存在");
        return await _db.ORDER_STATUS_LOGs.AsNoTracking()
            .Where(x => x.ORDER_TYPE == LogType && x.ORDER_ID == returnId)
            .OrderBy(x => x.CHANGE_TIME).ThenBy(x => x.LOG_ID)
            .Select(x => new OrderStatusLogDto
            {
                logId = x.LOG_ID,
                orderType = x.ORDER_TYPE,
                orderId = x.ORDER_ID,
                oldStatus = x.OLD_STATUS,
                newStatus = x.NEW_STATUS,
                operatorId = x.OPERATOR_ID,
                changeTime = x.CHANGE_TIME,
                remark = x.REMARK
            }).ToListAsync();
    }

    private async Task<(PURCHASE_ORDER purchase, List<ValidatedDetail> details)> ValidateRequestAsync(
        SavePurchaseReturnRequest request, int? excludedReturnId)
    {
        if (request.details.Count == 0) throw new ArgumentException("采购退货明细不能为空");
        if (!await _db.SYS_USERs.AsNoTracking().AnyAsync(x => x.USER_ID == request.operatorId && x.STATUS == "启用"))
            throw new KeyNotFoundException("经办人不存在或已禁用");
        var purchase = await _db.PURCHASE_ORDERs.AsNoTracking().Include(x => x.PURCHASE_ORDER_DETAILs)
            .FirstOrDefaultAsync(x => x.ORDER_ID == request.purchaseId) ?? throw new KeyNotFoundException("原采购单不存在");
        if (purchase.STATUS != "已入库") throw new InvalidOperationException("仅已入库采购单可以创建采购退货");
        if (!purchase.SUPPLIER_ID.HasValue) throw new InvalidOperationException("原采购单未关联供应商");

        var requested = request.details.GroupBy(x => x.productId)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.quantity));
        if (requested.Any(x => x.Key <= 0 || x.Value <= 0)) throw new ArgumentException("退货商品和数量必须大于 0");
        var purchased = purchase.PURCHASE_ORDER_DETAILs.GroupBy(x => x.PRODUCT_ID)
            .ToDictionary(x => x.Key, x => new
            {
                quantity = x.Sum(y => y.PURCHASE_QUANTITY ?? 0),
                price = x.Sum(y => (y.PURCHASE_PRICE ?? 0) * (y.PURCHASE_QUANTITY ?? 0)) /
                    Math.Max(1, x.Sum(y => y.PURCHASE_QUANTITY ?? 0))
            });
        if (requested.Keys.Except(purchased.Keys).Any()) throw new ArgumentException("退货商品不在原采购单中");

        var returnedQuery = _db.PURCHASE_RETURN_ORDER_DETAILs.AsNoTracking()
            .Where(x => x.RETURN.PURCHASE_ID == request.purchaseId && x.RETURN.STATUS != Voided);
        if (excludedReturnId.HasValue) returnedQuery = returnedQuery.Where(x => x.RETURN_ID != excludedReturnId.Value);
        var returned = await returnedQuery.GroupBy(x => x.PRODUCT_ID)
            .Select(x => new { productId = x.Key, quantity = x.Sum(y => y.QUANTITY) })
            .ToDictionaryAsync(x => x.productId, x => x.quantity);
        if (requested.Any(x => x.Value + returned.GetValueOrDefault(x.Key) > purchased[x.Key].quantity))
            throw new InvalidOperationException("累计采购退货数量不能超过原采购入库数量");

        var details = requested.Select(x => new ValidatedDetail(
            x.Key, x.Value, purchased[x.Key].price, Math.Round(purchased[x.Key].price * x.Value, 2))).ToList();
        return (purchase, details);
    }

    /// <summary>
    /// 按 UTF8 字节数截断字符串，保证不切断多字节字符（中文）
    /// </summary>
    private static string TruncateUtf8(string text, int maxBytes)
    {
        if (Encoding.UTF8.GetByteCount(text) <= maxBytes) return text;
        var builder = new StringBuilder();
        foreach (var c in text)
        {
            if (Encoding.UTF8.GetByteCount(builder.ToString()) + Encoding.UTF8.GetByteCount(c.ToString()) > maxBytes)
                break;
            builder.Append(c);
        }
        return builder.ToString();
    }

    private void AddLog(int returnId, string? oldStatus, string newStatus, int operatorId, string? remark)
    {
        _db.ORDER_STATUS_LOGs.Add(new ORDER_STATUS_LOG
        {
            ORDER_TYPE = LogType,
            ORDER_ID = returnId,
            OLD_STATUS = oldStatus,
            NEW_STATUS = newStatus,
            OPERATOR_ID = operatorId,
            CHANGE_TIME = DateTime.Now,
            REMARK = remark?.Trim()
        });
    }

    private static IQueryable<PurchaseReturnDto> Project(IQueryable<PURCHASE_RETURN_ORDER> query, bool details) =>
        query.Select(x => new PurchaseReturnDto
        {
            returnId = x.RETURN_ID,
            returnNo = x.RETURN_NO,
            purchaseId = x.PURCHASE_ID,
            purchaseCode = x.PURCHASE.ORDER_CODE,
            supplierId = x.SUPPLIER_ID,
            supplierName = x.SUPPLIER.SUPPLIER_NAME,
            operatorId = x.OPERATOR_ID,
            operatorName = x.OPERATOR.REAL_NAME ?? string.Empty,
            returnDate = x.RETURN_DATE,
            totalAmount = x.TOTAL_AMOUNT,
            status = x.STATUS ?? string.Empty,
            createTime = x.CREATE_TIME,
            updateTime = x.UPDATE_TIME,
            remark = x.REMARK,
            details = details ? x.PURCHASE_RETURN_ORDER_DETAILs.Select(d => new PurchaseReturnDetailDto
            {
                productId = d.PRODUCT_ID,
                productName = d.PRODUCT.PRODUCT_NAME,
                quantity = d.QUANTITY,
                returnPrice = d.RETURN_PRICE,
                subtotal = d.SUBTOTAL
            }).ToList() : null
        });

    private sealed record ValidatedDetail(int productId, int quantity, decimal returnPrice, decimal subtotal);
}
