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
        else if (request.redeemPoints > 0) throw new ArgumentException("非会员订单不能使用积分");

        decimal Price(PRODUCT x) => x.IS_PROMOTION == "是" && x.PROMOTION_PRICE.HasValue ? x.PROMOTION_PRICE.Value : x.SALE_PRICE ?? 0;
        if (products.Any(x => Price(x) <= 0)) throw new InvalidOperationException("订单包含未设置有效售价的商品");
        var total = products.Sum(x => Price(x) * quantities[x.PRODUCT_ID]);
        var discount = 0m;
        if (request.redeemPoints > 0)
        {
            if (config is null) throw new InvalidOperationException("当前没有启用的积分规则");
            if (request.redeemPoints < (config.REDEEM_MIN ?? 0)) throw new InvalidOperationException("使用积分低于最低抵扣数量");
            if (request.redeemPoints > (member!.POINTS ?? 0)) throw new InvalidOperationException("会员积分不足");
            discount = request.redeemPoints * config.REDEEM_RATE;
            var maxDiscount = total * (config.REDEEM_MAX_RATE ?? 0.5m);
            if (discount > maxDiscount) throw new InvalidOperationException("积分抵扣金额超过订单允许上限");
        }

        var order = new SALE_ORDER
        {
            SALE_NO = $"SO{now:yyyyMMddHHmmssfff}{Guid.NewGuid():N}"[..30],
            MEMBER_ID = request.memberId,
            USER_ID = userId,
            SALE_DATE = now,
            TOTAL_AMOUNT = total,
            DISCOUNT_AMOUNT = discount,
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
            member.TOTAL_AMOUNT = (member.TOTAL_AMOUNT ?? 0) + total - discount;
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetAsync(order.SALE_ID);
    }
}
