using System.Data;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class ReturnService : IReturnService
{
    private readonly AppDbContext _db;
    public ReturnService(AppDbContext db) => _db = db;

    public async Task<PageResult<ReturnOrderDto>> ListAsync(int page, int size, string? keyword, string? status)
    {
        page = Math.Max(1, page); size = Math.Clamp(size, 1, 100);
        var query = _db.RETURN_ORDERs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.RETURN_NO.Contains(value) || x.SALE.SALE_NO.Contains(value) ||
                (x.MEMBER != null && (x.MEMBER.MEMBER_NAME.Contains(value) || x.MEMBER.PHONE.Contains(value))));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.STATUS == status.Trim());
        var total = await query.CountAsync();
        var list = await Project(query.OrderByDescending(x => x.RETURN_DATE).ThenByDescending(x => x.RETURN_ID), false)
            .Skip((page - 1) * size).Take(size).ToListAsync();
        return new PageResult<ReturnOrderDto> { list = list, total = total, page = page, size = size };
    }

    public async Task<ReturnOrderDto> GetAsync(int returnId) =>
        await Project(_db.RETURN_ORDERs.AsNoTracking().Where(x => x.RETURN_ID == returnId), true).FirstOrDefaultAsync()
        ?? throw new KeyNotFoundException("退货单不存在");

    public async Task<ReturnOrderDto> CreateAsync(CreateReturnRequest request)
    {
        if (request.details.Count == 0) throw new ArgumentException("退货明细不能为空");
        if (!await _db.SYS_USERs.AsNoTracking().AnyAsync(x => x.USER_ID == request.operatorId))
            throw new KeyNotFoundException("经办人不存在");
        var sale = await _db.SALE_ORDERs.AsNoTracking().Include(x => x.SALE_ORDER_DETAILs)
            .FirstOrDefaultAsync(x => x.SALE_ID == request.saleId) ?? throw new KeyNotFoundException("销售单不存在");
        if (sale.STATUS != "已完成") throw new InvalidOperationException("仅已完成销售单可以退货");
        if (request.memberId.HasValue && request.memberId != sale.MEMBER_ID) throw new ArgumentException("退货会员与原销售单不一致");

        var requested = request.details.GroupBy(x => x.productId).ToDictionary(x => x.Key, x => x.Sum(d => d.quantity));
        var sold = sale.SALE_ORDER_DETAILs.ToDictionary(x => x.PRODUCT_ID, x => x.SALE_QUANTITY ?? 0);
        if (requested.Keys.Except(sold.Keys).Any()) throw new ArgumentException("退货商品不在原销售单中");
        var returned = await _db.RETURN_ORDER_DETAILs.AsNoTracking()
            .Where(x => x.RETURN.SALE_ID == request.saleId && x.RETURN.STATUS != "已拒绝")
            .GroupBy(x => x.PRODUCT_ID).Select(x => new { ProductId = x.Key, Quantity = x.Sum(d => d.QUANTITY) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);
        if (requested.Any(x => x.Value + returned.GetValueOrDefault(x.Key) > sold[x.Key]))
            throw new InvalidOperationException("累计退货数量不能超过原销售数量");

        var ratio = sale.TOTAL_AMOUNT.GetValueOrDefault() <= 0 ? 0 : sale.PAID_AMOUNT.GetValueOrDefault() / sale.TOTAL_AMOUNT!.Value;
        var now = DateTime.Now;
        var order = new RETURN_ORDER
        {
            RETURN_NO = $"RT{now:yyyyMMddHHmmssfff}{Guid.NewGuid():N}"[..30], SALE_ID = sale.SALE_ID,
            MEMBER_ID = sale.MEMBER_ID, OPERATOR_ID = request.operatorId, RETURN_DATE = now,
            STATUS = "待处理", CREATE_TIME = now, UPDATE_TIME = now, REMARK = request.remark?.Trim()
        };
        foreach (var item in requested)
        {
            var saleLine = sale.SALE_ORDER_DETAILs.First(x => x.PRODUCT_ID == item.Key);
            var refundPrice = Math.Round((saleLine.SALE_PRICE ?? 0) * ratio, 2, MidpointRounding.AwayFromZero);
            order.RETURN_ORDER_DETAILs.Add(new RETURN_ORDER_DETAIL
            {
                PRODUCT_ID = item.Key, QUANTITY = item.Value, REFUND_PRICE = refundPrice,
                SUBTOTAL = refundPrice * item.Value
            });
        }
        order.REFUND_AMOUNT = order.RETURN_ORDER_DETAILs.Sum(x => x.SUBTOTAL);
        _db.RETURN_ORDERs.Add(order);
        await _db.SaveChangesAsync();
        _db.ORDER_STATUS_LOGs.Add(new ORDER_STATUS_LOG
        {
            ORDER_TYPE = "退货单", ORDER_ID = order.RETURN_ID, OLD_STATUS = null, NEW_STATUS = "待处理",
            OPERATOR_ID = request.operatorId, CHANGE_TIME = now, REMARK = request.remark
        });
        await _db.SaveChangesAsync();
        return await GetAsync(order.RETURN_ID);
    }

    public async Task<ReturnOrderDto> ConfirmAsync(int returnId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM RETURN_ORDER WHERE RETURN_ID = {0} FOR UPDATE", returnId);
        var order = await _db.RETURN_ORDERs.Include(x => x.RETURN_ORDER_DETAILs).Include(x => x.SALE)
            .FirstOrDefaultAsync(x => x.RETURN_ID == returnId) ?? throw new KeyNotFoundException("退货单不存在");
        if (order.STATUS != "待处理") throw new InvalidOperationException("当前退货单已处理");
        var productIds = order.RETURN_ORDER_DETAILs.Select(x => x.PRODUCT_ID).OrderBy(x => x).ToList();
        var inList = string.Join(",", productIds);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM INVENTORY WHERE PRODUCT_ID IN (" + inList + ") ORDER BY PRODUCT_ID FOR UPDATE");
        var inventories = await _db.INVENTORies.Where(x => productIds.Contains(x.PRODUCT_ID))
            .OrderBy(x => x.WAREHOUSE_ID).ToListAsync();
        var now = DateTime.Now;
        foreach (var detail in order.RETURN_ORDER_DETAILs)
        {
            var inventory = inventories.FirstOrDefault(x => x.PRODUCT_ID == detail.PRODUCT_ID)
                ?? throw new InvalidOperationException($"商品 {detail.PRODUCT_ID} 没有库存记录，无法确定退回仓库");
            inventory.CURRENT_STOCK += detail.QUANTITY; inventory.LAST_UPDATE_TIME = now;
            _db.INVENTORY_RECORDs.Add(new INVENTORY_RECORD
            {
                PRODUCT_ID = detail.PRODUCT_ID, RECORD_TYPE = "退货", SOURCE_NO = order.RETURN_NO,
                CHANGE_QTY = detail.QUANTITY, REMAIN_QTY = inventory.CURRENT_STOCK,
                OPERATOR_ID = order.OPERATOR_ID, RECORD_TIME = now, REMARK = "销售退货入库"
            });
        }

        if (order.MEMBER_ID.HasValue)
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT * FROM MEMBER WHERE MEMBER_ID = {0} FOR UPDATE", order.MEMBER_ID.Value);
            var member = await _db.MEMBERs.FirstAsync(x => x.MEMBER_ID == order.MEMBER_ID.Value);
            var salePoints = await _db.POINT_RECORDs.AsNoTracking().Where(x => x.SALE_ID == order.SALE_ID).ToListAsync();
            var ratio = order.SALE.TOTAL_AMOUNT.GetValueOrDefault() <= 0 ? 0 :
                order.RETURN_ORDER_DETAILs.Sum(x => x.SUBTOTAL / Math.Max(0.0001m, order.SALE.PAID_AMOUNT.GetValueOrDefault()));
            ratio = Math.Clamp(ratio, 0, 1);
            var earned = salePoints.Where(x => x.CHANGE_POINTS > 0).Sum(x => x.CHANGE_POINTS);
            var redeemed = -salePoints.Where(x => x.CHANGE_POINTS < 0).Sum(x => x.CHANGE_POINTS);
            var reversal = (int)Math.Round((redeemed - earned) * ratio, MidpointRounding.AwayFromZero);
            if (reversal != 0)
            {
                member.POINTS = (member.POINTS ?? 0) + reversal;
                _db.POINT_RECORDs.Add(new POINT_RECORD
                {
                    MEMBER_ID = member.MEMBER_ID, SALE_ID = order.SALE_ID, CHANGE_TYPE = reversal > 0 ? "增加" : "扣减",
                    CHANGE_POINTS = reversal, REMAIN_POINTS = member.POINTS.Value, RECORD_TIME = now,
                    REMARK = $"退货单 {order.RETURN_NO} 积分冲销"
                });
            }
        }

        order.STATUS = "已完成"; order.UPDATE_TIME = now;
        _db.ORDER_STATUS_LOGs.Add(new ORDER_STATUS_LOG
        {
            ORDER_TYPE = "退货单", ORDER_ID = returnId, OLD_STATUS = "待处理", NEW_STATUS = "已完成",
            OPERATOR_ID = order.OPERATOR_ID, CHANGE_TIME = now, REMARK = "确认退货并完成退款、入库及积分冲销"
        });
        await _db.SaveChangesAsync();
        if (order.MEMBER_ID.HasValue) await MemberLevelPolicy.RefreshAsync(_db, order.MEMBER_ID.Value, now);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(returnId);
    }

    public async Task<IReadOnlyList<OrderStatusLogDto>> GetTimelineAsync(int returnId)
    {
        if (!await _db.RETURN_ORDERs.AsNoTracking().AnyAsync(x => x.RETURN_ID == returnId)) throw new KeyNotFoundException("退货单不存在");
        return await _db.ORDER_STATUS_LOGs.AsNoTracking().Where(x => x.ORDER_TYPE == "退货单" && x.ORDER_ID == returnId)
            .OrderBy(x => x.CHANGE_TIME).ThenBy(x => x.LOG_ID).Select(x => new OrderStatusLogDto
            {
                logId = x.LOG_ID, orderType = x.ORDER_TYPE, orderId = x.ORDER_ID, oldStatus = x.OLD_STATUS,
                newStatus = x.NEW_STATUS, operatorId = x.OPERATOR_ID, changeTime = x.CHANGE_TIME, remark = x.REMARK
            }).ToListAsync();
    }

    private static IQueryable<ReturnOrderDto> Project(IQueryable<RETURN_ORDER> query, bool details) => query.Select(x => new ReturnOrderDto
    {
        returnId = x.RETURN_ID, returnNo = x.RETURN_NO, saleId = x.SALE_ID, saleNo = x.SALE.SALE_NO,
        memberId = x.MEMBER_ID, memberName = x.MEMBER == null ? null : x.MEMBER.MEMBER_NAME,
        operatorId = x.OPERATOR_ID, operatorName = x.OPERATOR.REAL_NAME, returnDate = x.RETURN_DATE,
        refundAmount = x.REFUND_AMOUNT, status = x.STATUS, createTime = x.CREATE_TIME, updateTime = x.UPDATE_TIME,
        remark = x.REMARK, details = details ? x.RETURN_ORDER_DETAILs.Select(d => new ReturnOrderDetailDto
        {
            productId = d.PRODUCT_ID, productName = d.PRODUCT.PRODUCT_NAME, quantity = d.QUANTITY,
            refundPrice = d.REFUND_PRICE, subtotal = d.SUBTOTAL
        }).ToList() : null
    });
}
