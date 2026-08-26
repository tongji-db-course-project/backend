using System.Data;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class SaleService : ISaleService
{
    private readonly AppDbContext _db;
    public SaleService(AppDbContext db) => _db = db;

    public async Task<PageResult<SaleListItemDto>> ListAsync(
        int page, int size, string? keyword, string? status, DateTime? startDate, DateTime? endDate)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 100);
        if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
            throw new ArgumentException("开始日期不能晚于结束日期");

        var query = _db.SALE_ORDERs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.SALE_NO.Contains(value) ||
                (x.MEMBER != null && (x.MEMBER.MEMBER_NAME.Contains(value) || x.MEMBER.PHONE.Contains(value))));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.STATUS == status.Trim());
        if (startDate.HasValue) query = query.Where(x => x.SALE_DATE >= startDate.Value.Date);
        if (endDate.HasValue) query = query.Where(x => x.SALE_DATE < endDate.Value.Date.AddDays(1));

        var total = await query.CountAsync();
        var list = await query.OrderByDescending(x => x.SALE_DATE).ThenByDescending(x => x.SALE_ID)
            .Skip((page - 1) * size).Take(size).Select(x => new SaleListItemDto
            {
                saleId = x.SALE_ID,
                saleNo = x.SALE_NO,
                memberId = x.MEMBER_ID,
                memberName = x.MEMBER == null ? null : x.MEMBER.MEMBER_NAME,
                userId = x.USER_ID,
                cashierName = x.USER.REAL_NAME,
                saleDate = x.SALE_DATE,
                totalAmount = x.TOTAL_AMOUNT ?? 0,
                discountAmount = x.DISCOUNT_AMOUNT ?? 0,
                paidAmount = x.PAID_AMOUNT ?? 0,
                payType = x.PAY_TYPE,
                status = x.STATUS
            }).ToListAsync();
        return new PageResult<SaleListItemDto> { list = list, total = total, page = page, size = size };
    }

    public async Task<SaleDetailDto> GetAsync(int saleId)
    {
        return await _db.SALE_ORDERs.AsNoTracking().Where(x => x.SALE_ID == saleId)
            .Select(x => new SaleDetailDto
            {
                saleId = x.SALE_ID,
                saleNo = x.SALE_NO,
                memberId = x.MEMBER_ID,
                memberName = x.MEMBER == null ? null : x.MEMBER.MEMBER_NAME,
                userId = x.USER_ID,
                cashierName = x.USER.REAL_NAME,
                saleDate = x.SALE_DATE,
                totalAmount = x.TOTAL_AMOUNT ?? 0,
                discountAmount = x.DISCOUNT_AMOUNT ?? 0,
                paidAmount = x.PAID_AMOUNT ?? 0,
                payType = x.PAY_TYPE,
                status = x.STATUS,
                couponId = x.MEMBER_COUPONs.Select(c => (int?)c.COUPON_ID).FirstOrDefault(),
                couponName = x.MEMBER_COUPONs.Select(c => c.TEMPLATE.COUPON_NAME).FirstOrDefault(),
                promotionDiscount = x.PROMOTION_DISCOUNT ?? 0,
                memberDiscount = x.MEMBER_DISCOUNT ?? 0,
                couponDeduct = x.COUPON_DEDUCT ?? 0,
                pointDeduct = x.POINT_DEDUCT ?? 0,
                redeemedPoints = x.POINT_RECORDs.Where(p => p.CHANGE_TYPE == "抵现").Sum(p => (int?)-p.CHANGE_POINTS) ?? 0,
                earnedPoints = x.POINT_RECORDs.Where(p => p.CHANGE_TYPE == "增加").Sum(p => (int?)p.CHANGE_POINTS) ?? 0,
                items = x.SALE_ORDER_DETAILs.Select(d => new SaleDetailItemDto
                {
                    productId = d.PRODUCT_ID,
                    productName = d.PRODUCT.PRODUCT_NAME,
                    quantity = d.SALE_QUANTITY ?? 0,
                    salePrice = d.SALE_PRICE ?? 0,
                    subtotal = (d.SALE_PRICE ?? 0) * (d.SALE_QUANTITY ?? 0)
                }).ToList()
            }).FirstOrDefaultAsync() ?? throw new KeyNotFoundException("销售单不存在");
    }

    public async Task<SaleDetailDto> CreateAsync(CreateSaleRequest request, int userId)
    {
        if (request.items.Count == 0) throw new ArgumentException("销售商品不能为空");
        if (string.IsNullOrWhiteSpace(request.payType)) throw new ArgumentException("支付方式不能为空");
        var quantities = request.items.GroupBy(x => x.productId).ToDictionary(x => x.Key, x => x.Sum(i => i.quantity));
        if (quantities.Any(x => x.Key <= 0 || x.Value <= 0)) throw new ArgumentException("商品和数量必须大于 0");

        var warehouseExists = await _db.WAREHOUSEs.AsNoTracking().AnyAsync(x => x.WAREHOUSE_ID == request.warehouseId);
        if (!warehouseExists) throw new KeyNotFoundException("仓库不存在");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var now = DateTime.Now;

        // 主键由数据库 Identity 生成（见 AppDbContext.IdentityOverrides.cs），不再手工分配，避免并发下主键冲突。
        var products = await _db.PRODUCTs.Where(x => quantities.Keys.Contains(x.PRODUCT_ID)).ToListAsync();
        if (products.Count != quantities.Count) throw new KeyNotFoundException("部分商品不存在");
        if (products.Any(x => x.STATUS != "在售")) throw new InvalidOperationException("订单包含非在售商品");

        // FOR UPDATE 行锁：串行化对同一库存行的并发扣减，避免超卖。
        // 注意：FromSqlRaw 会被 EF 包成 FROM ( ... ) b 子查询，Oracle 不允许子查询里用 FOR UPDATE，
        // 因此用 ExecuteSqlRawAsync 直接下发锁语句（非查询 SQL 不被包装）。
        // productIds 排序保证多个并发销售按一致顺序加锁，避免死锁；
        // inList 只由校验过的整数拼接而成，无注入风险；warehouseId 走参数化占位符。
        var productIds = quantities.Keys.OrderBy(x => x).ToList();
        var inList = string.Join(",", productIds);
        var lockSql = "SELECT * FROM INVENTORY WHERE WAREHOUSE_ID = {0} AND PRODUCT_ID IN (" + inList + ") ORDER BY PRODUCT_ID FOR UPDATE";
        await _db.Database.ExecuteSqlRawAsync(lockSql, request.warehouseId);
        var inventories = await _db.INVENTORies
            .Where(x => x.WAREHOUSE_ID == request.warehouseId && quantities.Keys.Contains(x.PRODUCT_ID))
            .ToListAsync();
        if (inventories.Count != quantities.Count) throw new InvalidOperationException("部分商品在指定仓库没有库存记录");
        foreach (var inventory in inventories)
            if (inventory.CURRENT_STOCK < quantities[inventory.PRODUCT_ID])
                throw new InvalidOperationException($"商品“{products.First(x => x.PRODUCT_ID == inventory.PRODUCT_ID).PRODUCT_NAME}”库存不足");

        MEMBER? member = null;
        MEMBER_COUPON? coupon = null;
        POINT_CONFIG? config = null;
        if (request.memberId.HasValue)
        {
            // FOR UPDATE 行锁：串行化对同一会员积分余额的并发变动（抵现 + 获赠），避免余额判断与写入竞争。
            await _db.Database.ExecuteSqlRawAsync("SELECT * FROM MEMBER WHERE MEMBER_ID = {0} FOR UPDATE", request.memberId.Value);
            member = await _db.MEMBERs.FirstOrDefaultAsync(x => x.MEMBER_ID == request.memberId.Value)
                ?? throw new KeyNotFoundException("会员不存在");
            if (member.STATUS != "启用") throw new InvalidOperationException("会员状态不可用");
            config = await _db.POINT_CONFIGs.AsNoTracking().Where(x => x.STATUS == "启用")
                .OrderByDescending(x => x.UPDATE_TIME).FirstOrDefaultAsync();
        }
        else if (request.redeemPoints > 0 || request.couponId.HasValue) throw new ArgumentException("非会员订单不能使用积分或优惠券");

        decimal Price(PRODUCT x) => x.IS_PROMOTION == "是" && x.PROMOTION_PRICE.HasValue ? x.PROMOTION_PRICE.Value : x.SALE_PRICE ?? 0;
        if (products.Any(x => Price(x) <= 0)) throw new InvalidOperationException("订单包含未设置有效售价的商品");
        var total = products.Sum(x => Price(x) * quantities[x.PRODUCT_ID]);
        var promotionDiscount = products.Sum(x => Math.Max(0, (x.SALE_PRICE ?? 0) - Price(x)) * quantities[x.PRODUCT_ID]);
        var memberRate = member?.LEVEL_NAME switch
        {
            "钻石会员" or "钻石" => 0.90m,
            "黄金会员" or "黄金" => 0.95m,
            _ => 1m
        };
        var memberDiscount = Math.Round(total * (1 - memberRate), 2, MidpointRounding.AwayFromZero);
        var couponDiscount = 0m;
        if (request.couponId.HasValue)
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT * FROM MEMBER_COUPON WHERE COUPON_ID = {0} FOR UPDATE", request.couponId.Value);
            coupon = await _db.MEMBER_COUPONs.Include(x => x.TEMPLATE)
                .FirstOrDefaultAsync(x => x.COUPON_ID == request.couponId.Value)
                ?? throw new KeyNotFoundException("优惠券不存在");
            if (coupon.MEMBER_ID != member!.MEMBER_ID) throw new InvalidOperationException("优惠券不属于当前会员");
            if (coupon.STATUS != "未使用") throw new InvalidOperationException("优惠券当前不可使用");
            if (coupon.TEMPLATE.STATUS != "启用") throw new InvalidOperationException("优惠券模板已停用");
            if (coupon.RECEIVE_TIME.HasValue && coupon.RECEIVE_TIME.Value.Date.AddDays(coupon.TEMPLATE.VALID_DAYS) < now.Date)
                throw new InvalidOperationException("优惠券已过期");
            if (total < (coupon.TEMPLATE.MIN_AMOUNT ?? 0)) throw new InvalidOperationException("订单金额未达到优惠券使用门槛");
            var amountAfterMember = total - memberDiscount;
            couponDiscount = coupon.TEMPLATE.COUPON_TYPE == "折扣券"
                ? amountAfterMember * (1 - coupon.TEMPLATE.FACE_VALUE)
                : coupon.TEMPLATE.FACE_VALUE;
            if (coupon.TEMPLATE.MAX_DISCOUNT.HasValue)
                couponDiscount = Math.Min(couponDiscount, coupon.TEMPLATE.MAX_DISCOUNT.Value);
            couponDiscount = Math.Round(Math.Clamp(couponDiscount, 0, amountAfterMember), 2, MidpointRounding.AwayFromZero);
        }
        var pointDiscount = 0m;
        if (request.redeemPoints > 0)
        {
            if (config is null) throw new InvalidOperationException("当前没有启用的积分规则");
            if (request.redeemPoints < (config.REDEEM_MIN ?? 0)) throw new InvalidOperationException("使用积分低于最低抵扣数量");
            if (request.redeemPoints > (member!.POINTS ?? 0)) throw new InvalidOperationException("会员积分不足");
            pointDiscount = request.redeemPoints * config.REDEEM_RATE;
            var maxDiscount = (total - memberDiscount - couponDiscount) * (config.REDEEM_MAX_RATE ?? 0.5m);
            if (pointDiscount > maxDiscount) throw new InvalidOperationException("积分抵扣金额超过订单允许上限");
        }
        var discount = memberDiscount + couponDiscount + pointDiscount;

        var order = new SALE_ORDER
        {
            SALE_NO = $"SO{now:yyyyMMddHHmmssfff}{Guid.NewGuid():N}"[..30],
            MEMBER_ID = request.memberId,
            USER_ID = userId,
            SALE_DATE = now,
            TOTAL_AMOUNT = total,
            DISCOUNT_AMOUNT = discount,
            PROMOTION_DISCOUNT = promotionDiscount,
            MEMBER_DISCOUNT = memberDiscount,
            COUPON_DEDUCT = couponDiscount,
            POINT_DEDUCT = pointDiscount,
            PAID_AMOUNT = total - discount,
            PAY_TYPE = request.payType.Trim(),
            STATUS = "已完成",
            CREATE_TIME = now,
            UPDATE_TIME = now
        };
        foreach (var product in products)
            order.SALE_ORDER_DETAILs.Add(new SALE_ORDER_DETAIL
            {
                PRODUCT_ID = product.PRODUCT_ID,
                SALE_QUANTITY = quantities[product.PRODUCT_ID],
                SALE_PRICE = Price(product)
            });
        _db.SALE_ORDERs.Add(order);

        if (coupon is not null)
        {
            coupon.STATUS = "已使用";
            coupon.USE_TIME = now;
            coupon.SALE = order;
        }

        foreach (var inventory in inventories)
        {
            var quantity = quantities[inventory.PRODUCT_ID];
            inventory.CURRENT_STOCK -= quantity;
            inventory.LAST_UPDATE_TIME = now;
            _db.INVENTORY_RECORDs.Add(new INVENTORY_RECORD
            {
                PRODUCT_ID = inventory.PRODUCT_ID,
                RECORD_TYPE = "销售",
                SOURCE_NO = order.SALE_NO,
                CHANGE_QTY = -quantity,
                REMAIN_QTY = inventory.CURRENT_STOCK,
                OPERATOR_ID = userId,
                RECORD_TIME = now,
                REMARK = "销售出库"
            });
        }

        if (member is not null)
        {
            var remain = member.POINTS ?? 0;
            if (request.redeemPoints > 0)
            {
                remain -= request.redeemPoints;
                order.POINT_RECORDs.Add(new POINT_RECORD
                {
                    MEMBER_ID = member.MEMBER_ID,
                    CHANGE_TYPE = "抵现",
                    CHANGE_POINTS = -request.redeemPoints,
                    REMAIN_POINTS = remain,
                    RECORD_TIME = now,
                    REMARK = "销售积分抵现"
                });
            }
            var earned = config is null ? 0 : (int)Math.Floor((total - discount) * config.EARN_RATE);
            if (earned > 0)
            {
                remain += earned;
                order.POINT_RECORDs.Add(new POINT_RECORD
                {
                    MEMBER_ID = member.MEMBER_ID,
                    CHANGE_TYPE = "增加",
                    CHANGE_POINTS = earned,
                    REMAIN_POINTS = remain,
                    RECORD_TIME = now,
                    REMARK = "销售获得积分"
                });
            }
            member.POINTS = remain;
        }

        await _db.SaveChangesAsync();
        _db.ORDER_STATUS_LOGs.Add(new ORDER_STATUS_LOG
        {
            ORDER_TYPE = "销售单", ORDER_ID = order.SALE_ID, OLD_STATUS = null, NEW_STATUS = "已完成",
            OPERATOR_ID = userId, CHANGE_TIME = now, REMARK = "POS 收银结算"
        });
        if (member is not null)
        {
            await MemberLevelPolicy.RefreshAsync(_db, member.MEMBER_ID, now);
        }
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(order.SALE_ID);
    }

    public async Task CancelAsync(int saleId, int operatorId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await _db.Database.ExecuteSqlRawAsync("SELECT * FROM SALE_ORDER WHERE SALE_ID = {0} FOR UPDATE", saleId);
        var sale = await _db.SALE_ORDERs.Include(x => x.SALE_ORDER_DETAILs).FirstOrDefaultAsync(x => x.SALE_ID == saleId)
            ?? throw new KeyNotFoundException("销售单不存在");
        if (sale.STATUS != "已完成") throw new InvalidOperationException("当前销售单不能作废");
        if (await _db.RETURN_ORDERs.AnyAsync(x => x.SALE_ID == saleId && x.STATUS != "已拒绝"))
            throw new InvalidOperationException("销售单存在退货记录，不能直接作废");
        var productIds = sale.SALE_ORDER_DETAILs.Select(x => x.PRODUCT_ID).OrderBy(x => x).ToList();
        var inList = string.Join(",", productIds);
        var warehouseId = await GetDefaultWarehouseIdAsync();
        await _db.Database.ExecuteSqlRawAsync(
            "SELECT * FROM INVENTORY WHERE WAREHOUSE_ID = {0} AND PRODUCT_ID IN (" + inList + ") ORDER BY PRODUCT_ID FOR UPDATE",
            warehouseId);
        var inventories = await _db.INVENTORies.Where(x => x.WAREHOUSE_ID == warehouseId && productIds.Contains(x.PRODUCT_ID)).ToListAsync();
        var now = DateTime.Now;
        foreach (var detail in sale.SALE_ORDER_DETAILs)
        {
            var inventory = inventories.FirstOrDefault(x => x.PRODUCT_ID == detail.PRODUCT_ID)
                ?? throw new InvalidOperationException($"商品 {detail.PRODUCT_ID} 没有库存记录");
            var quantity = detail.SALE_QUANTITY ?? 0;
            inventory.CURRENT_STOCK += quantity; inventory.LAST_UPDATE_TIME = now;
            _db.INVENTORY_RECORDs.Add(new INVENTORY_RECORD
            {
                PRODUCT_ID = detail.PRODUCT_ID, RECORD_TYPE = "销售作废", SOURCE_NO = sale.SALE_NO,
                CHANGE_QTY = quantity, REMAIN_QTY = inventory.CURRENT_STOCK, OPERATOR_ID = operatorId,
                RECORD_TIME = now, REMARK = "销售单作废恢复库存"
            });
        }
        if (sale.MEMBER_ID.HasValue)
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT * FROM MEMBER WHERE MEMBER_ID = {0} FOR UPDATE", sale.MEMBER_ID.Value);
            var member = await _db.MEMBERs.FirstAsync(x => x.MEMBER_ID == sale.MEMBER_ID.Value);
            var originalPoints = await _db.POINT_RECORDs.AsNoTracking().Where(x => x.SALE_ID == saleId).SumAsync(x => (int?)x.CHANGE_POINTS) ?? 0;
            if (originalPoints != 0)
            {
                member.POINTS = (member.POINTS ?? 0) - originalPoints;
                _db.POINT_RECORDs.Add(new POINT_RECORD
                {
                    MEMBER_ID = member.MEMBER_ID, SALE_ID = saleId, CHANGE_TYPE = originalPoints > 0 ? "扣减" : "增加",
                    CHANGE_POINTS = -originalPoints, REMAIN_POINTS = member.POINTS.Value, RECORD_TIME = now, REMARK = "销售单作废积分冲销"
                });
            }
        }
        var usedCoupons = await _db.MEMBER_COUPONs.Where(x => x.SALE_ID == saleId).ToListAsync();
        foreach (var coupon in usedCoupons)
        {
            coupon.STATUS = "未使用";
            coupon.USE_TIME = null;
            coupon.SALE_ID = null;
        }
        sale.STATUS = "已取消"; sale.UPDATE_TIME = now;
        _db.ORDER_STATUS_LOGs.Add(new ORDER_STATUS_LOG
        {
            ORDER_TYPE = "销售单", ORDER_ID = saleId, OLD_STATUS = "已完成", NEW_STATUS = "已取消",
            OPERATOR_ID = operatorId, CHANGE_TIME = now, REMARK = "销售单作废"
        });
        await _db.SaveChangesAsync();
        if (sale.MEMBER_ID.HasValue) await MemberLevelPolicy.RefreshAsync(_db, sale.MEMBER_ID.Value, now);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<IReadOnlyList<OrderStatusLogDto>> GetTimelineAsync(int saleId)
    {
        if (!await _db.SALE_ORDERs.AsNoTracking().AnyAsync(x => x.SALE_ID == saleId)) throw new KeyNotFoundException("销售单不存在");
        return await _db.ORDER_STATUS_LOGs.AsNoTracking().Where(x => x.ORDER_TYPE == "销售单" && x.ORDER_ID == saleId)
            .OrderBy(x => x.CHANGE_TIME).ThenBy(x => x.LOG_ID).Select(x => new OrderStatusLogDto
            {
                logId = x.LOG_ID, orderType = x.ORDER_TYPE, orderId = x.ORDER_ID, oldStatus = x.OLD_STATUS,
                newStatus = x.NEW_STATUS, operatorId = x.OPERATOR_ID, changeTime = x.CHANGE_TIME, remark = x.REMARK
            }).ToListAsync();
    }

    // 单仓库模式：作废恢复库存时固定退回唯一启用仓库，避免因未指定仓库而恢复到任意一条库存记录上。
    private async Task<int> GetDefaultWarehouseIdAsync()
    {
        var warehouseIds = await _db.WAREHOUSEs.AsNoTracking()
            .Where(x => x.STATUS == "启用")
            .OrderBy(x => x.WAREHOUSE_ID)
            .Select(x => x.WAREHOUSE_ID)
            .Take(2)
            .ToListAsync();
        return warehouseIds.Count switch
        {
            0 => throw new InvalidOperationException("系统未配置启用仓库"),
            > 1 => throw new InvalidOperationException("单仓库模式下只能配置一个启用仓库"),
            _ => warehouseIds[0]
        };
    }
}
