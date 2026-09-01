using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

/// <summary>
/// 统计业务实现
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly AppDbContext _db;

    public StatisticsService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 按日期统计销售数据（按天分组，返回每天的统计）
    /// </summary>
    public async Task<List<SalesStatistics>> GetDailySalesStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1).AddSeconds(-1);

        // 按天分组统计销售订单
        var dailySales = await _db.SALE_ORDERs.AsNoTracking()
            .Where(o => o.STATUS == "已完成" && o.SALE_DATE >= start && o.SALE_DATE <= end)
            .GroupBy(o => o.SALE_DATE!.Value.Date)
            .Select(g => new
            {
                StatDate = g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(o => o.TOTAL_AMOUNT ?? 0),
                PaidAmount = g.Sum(o => o.PAID_AMOUNT ?? 0)
            })
            .ToListAsync();

        // 按天分组统计退款金额
        var dailyRefunds = await _db.RETURN_ORDERs.AsNoTracking()
            .Where(r => r.STATUS == "已完成" && r.RETURN_DATE >= start && r.RETURN_DATE <= end)
            .GroupBy(r => r.RETURN_DATE.Date)
            .Select(g => new
            {
                StatDate = g.Key,
                RefundAmount = g.Sum(r => r.REFUND_AMOUNT)
            })
            .ToListAsync();

        // 合并销售和退款数据
        var refundDict = dailyRefunds.ToDictionary(r => r.StatDate, r => r.RefundAmount);
        var allDates = dailySales.Select(s => s.StatDate)
            .Union(refundDict.Keys)
            .OrderBy(d => d)
            .ToList();

        return allDates.Select(date =>
        {
            var sale = dailySales.FirstOrDefault(s => s.StatDate == date);
            var refundAmount = refundDict.TryGetValue(date, out var r) ? r : 0;
            var paidAmount = sale?.PaidAmount ?? 0;
            return new SalesStatistics
            {
                StatDate = date.ToString("yyyy-MM-dd"),
                OrderCount = sale?.OrderCount ?? 0,
                TotalAmount = Math.Round(sale?.TotalAmount ?? 0, 2),
                PaidAmount = Math.Round(paidAmount, 2),
                RefundAmount = Math.Round(refundAmount, 2),
                NetAmount = Math.Round(paidAmount - refundAmount, 2)
            };
        }).ToList();
    }

    /// <summary>
    /// 按月份统计销售数据（按月分组，返回每月的统计）
    /// </summary>
    public async Task<List<MonthlySalesStatistics>> GetMonthlySalesStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1).AddSeconds(-1);

        // 按月分组统计销售订单
        var monthlySales = await _db.SALE_ORDERs.AsNoTracking()
            .Where(o => o.STATUS == "已完成" && o.SALE_DATE >= start && o.SALE_DATE <= end)
            .GroupBy(o => new { o.SALE_DATE!.Value.Year, o.SALE_DATE!.Value.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(o => o.TOTAL_AMOUNT ?? 0),
                PaidAmount = g.Sum(o => o.PAID_AMOUNT ?? 0)
            })
            .ToListAsync();

        // 按月分组统计退款金额
        var monthlyRefunds = await _db.RETURN_ORDERs.AsNoTracking()
            .Where(r => r.STATUS == "已完成" && r.RETURN_DATE >= start && r.RETURN_DATE <= end)
            .GroupBy(r => new { r.RETURN_DATE.Year, r.RETURN_DATE.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                RefundAmount = g.Sum(r => r.REFUND_AMOUNT)
            })
            .ToListAsync();

        // 合并销售和退款数据
        var refundDict = monthlyRefunds.ToDictionary(r => (r.Year, r.Month), r => r.RefundAmount);
        var allMonths = monthlySales.Select(s => (s.Year, s.Month))
            .Union(refundDict.Keys)
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

        return allMonths.Select(m =>
        {
            var sale = monthlySales.FirstOrDefault(s => s.Year == m.Year && s.Month == m.Month);
            var refundAmount = refundDict.TryGetValue(m, out var r) ? r : 0;
            var paidAmount = sale?.PaidAmount ?? 0;
            return new MonthlySalesStatistics
            {
                Month = $"{m.Year}-{m.Month:D2}",
                OrderCount = sale?.OrderCount ?? 0,
                TotalAmount = Math.Round(sale?.TotalAmount ?? 0, 2),
                PaidAmount = Math.Round(paidAmount, 2),
                RefundAmount = Math.Round(refundAmount, 2),
                NetAmount = Math.Round(paidAmount - refundAmount, 2)
            };
        }).ToList();
    }

    /// <summary>
    /// 查询商品销量排行
    /// </summary>
    public async Task<List<ProductRank>> GetProductRankAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1).AddSeconds(-1);

        var productRanks = await (
            from d in _db.SALE_ORDER_DETAILs.AsNoTracking()
            join s in _db.SALE_ORDERs on d.SALE_ID equals s.SALE_ID
            join p in _db.PRODUCTs on d.PRODUCT_ID equals p.PRODUCT_ID
            where s.STATUS == "已完成" && s.SALE_DATE >= start && s.SALE_DATE <= end
            group new { d, p } by new { p.PRODUCT_ID, p.PRODUCT_NAME } into g
            select new
            {
                ProductId = g.Key.PRODUCT_ID,
                ProductName = g.Key.PRODUCT_NAME,
                SaleQuantity = g.Sum(x => x.d.SALE_QUANTITY),
                SaleAmount = g.Sum(x => x.d.SALE_QUANTITY * x.d.SALE_PRICE)
            })
            .OrderByDescending(x => x.SaleQuantity)
            .Take(10)
            .ToListAsync();

        return productRanks.Select(x => new ProductRank
        {
            ProductId = x.ProductId,
            ProductName = x.ProductName ?? string.Empty,
            SaleQuantity = (int)(x.SaleQuantity ?? 0),
            SaleAmount = Math.Round(x.SaleAmount ?? 0, 2)
        }).ToList();
    }

    /// <summary>
    /// 商品毛利分析
    /// </summary>
    public async Task<ProfitStatistics> GetProfitStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1).AddSeconds(-1);

        var result = await (
            from d in _db.SALE_ORDER_DETAILs.AsNoTracking()
            join s in _db.SALE_ORDERs on d.SALE_ID equals s.SALE_ID
            join p in _db.PRODUCTs on d.PRODUCT_ID equals p.PRODUCT_ID
            where s.STATUS == "已完成" && s.SALE_DATE >= start && s.SALE_DATE <= end
            select new
            {
                SaleAmount = d.SALE_QUANTITY * d.SALE_PRICE,
                PurchaseCost = d.SALE_QUANTITY * p.PURCHASE_PRICE
            })
            .GroupBy(x => 1)
            .Select(g => new
            {
                TotalSaleAmount = g.Sum(x => x.SaleAmount),
                TotalPurchaseCost = g.Sum(x => x.PurchaseCost)
            })
            .FirstOrDefaultAsync();

        var totalSaleAmount = Math.Round(result?.TotalSaleAmount ?? 0, 2);
        var totalPurchaseCost = Math.Round(result?.TotalPurchaseCost ?? 0, 2);
        var grossProfit = Math.Round(totalSaleAmount - totalPurchaseCost, 2);
        var grossProfitRate = totalSaleAmount > 0 ? Math.Round(grossProfit / totalSaleAmount, 3) : 0;

        return new ProfitStatistics
        {
            TotalSaleAmount = totalSaleAmount,
            TotalPurchaseCost = totalPurchaseCost,
            GrossProfit = grossProfit,
            GrossProfitRate = grossProfitRate
        };
    }

    /// <summary>
    /// 库存统计 - 统计库存总量和低库存数量
    /// </summary>
    public async Task<InventoryStatistics> GetInventoryStatisticsAsync(DateTime? startDate, DateTime? endDate)
    {
        var inventories = await _db.INVENTORies.AsNoTracking()
            .Select(i => new
            {
                i.PRODUCT_ID,
                i.CURRENT_STOCK,
                StockWarning = i.PRODUCT!.STOCK_WARNING
            })
            .ToListAsync();

        var productCount = inventories.Select(i => i.PRODUCT_ID).Distinct().LongCount();
        var totalStock = (long)inventories.Sum(i => i.CURRENT_STOCK);
        var warningProductCount = inventories
            .Where(i => i.StockWarning.HasValue && i.CURRENT_STOCK < i.StockWarning.Value)
            .Select(i => i.PRODUCT_ID)
            .Distinct()
            .LongCount();

        var warehouseCount = await _db.WAREHOUSEs.AsNoTracking().LongCountAsync();

        return new InventoryStatistics
        {
            ProductCount = productCount,
            TotalStock = totalStock,
            WarningProductCount = warningProductCount,
            WarehouseCount = warehouseCount
        };
    }

    /// <summary>
    /// 会员消费统计
    /// </summary>
    public async Task<MemberStatistics> GetMemberStatisticsAsync(DateTime? startDate, DateTime? endDate)
    {
        var memberCount = await _db.MEMBERs.AsNoTracking()
            .LongCountAsync();

        IQueryable<SALE_ORDER> orders = _db.SALE_ORDERs.AsNoTracking()
            .Where(o => o.MEMBER_ID != null && o.STATUS == "已完成");

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            orders = orders.Where(o => o.SALE_DATE >= start);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1).AddSeconds(-1);
            orders = orders.Where(o => o.SALE_DATE <= end);
        }

        var memberOrders = await orders.ToListAsync();

        var activeMemberCount = memberOrders.Select(o => o.MEMBER_ID!.Value).Distinct().LongCount();
        var memberSaleAmount = (double)memberOrders.Sum(o => o.PAID_AMOUNT ?? 0);
        var averageSaleAmount = activeMemberCount > 0 ? Math.Round(memberSaleAmount / activeMemberCount, 2) : 0;

        return new MemberStatistics
        {
            MemberCount = memberCount,
            ActiveMemberCount = activeMemberCount,
            MemberSaleAmount = Math.Round(memberSaleAmount, 2),
            AverageSaleAmount = averageSaleAmount
        };
    }

    public async Task<List<ProductProfitRankDto>> GetProductProfitRankAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date; var end = endDate.Date.AddDays(1);
        var sales = await _db.SALE_ORDER_DETAILs.AsNoTracking()
            .Where(x => x.SALE.STATUS == "已完成" && x.SALE.SALE_DATE >= start && x.SALE.SALE_DATE < end)
            .GroupBy(x => new { x.PRODUCT_ID, x.PRODUCT.PRODUCT_NAME, x.PRODUCT.PURCHASE_PRICE })
            .Select(x => new
            {
                x.Key.PRODUCT_ID, x.Key.PRODUCT_NAME, PurchasePrice = x.Key.PURCHASE_PRICE ?? 0,
                Quantity = x.Sum(d => d.SALE_QUANTITY ?? 0), Amount = x.Sum(d => (d.SALE_QUANTITY ?? 0) * (d.SALE_PRICE ?? 0))
            }).ToListAsync();
        var returns = await _db.RETURN_ORDER_DETAILs.AsNoTracking()
            .Where(x => x.RETURN.STATUS == "已完成" && x.RETURN.RETURN_DATE >= start && x.RETURN.RETURN_DATE < end)
            .GroupBy(x => x.PRODUCT_ID).Select(x => new
            {
                ProductId = x.Key, Quantity = x.Sum(d => d.QUANTITY), Amount = x.Sum(d => d.SUBTOTAL)
            }).ToDictionaryAsync(x => x.ProductId);
        var list = sales.Select(x =>
        {
            var returned = returns.GetValueOrDefault(x.PRODUCT_ID);
            var quantity = Math.Max(0, x.Quantity - (returned?.Quantity ?? 0));
            var revenue = Math.Max(0, x.Amount - (returned?.Amount ?? 0));
            var cost = quantity * x.PurchasePrice;
            return new ProductProfitRankDto
            {
                productId = x.PRODUCT_ID, productName = x.PRODUCT_NAME, netSaleQuantity = quantity,
                netSaleAmount = Math.Round(revenue, 2), purchaseCost = Math.Round(cost, 2),
                grossProfit = Math.Round(revenue - cost, 2),
                grossProfitRate = revenue > 0 ? Math.Round((revenue - cost) / revenue, 4) : 0
            };
        }).ToList();
        var totalProfit = list.Sum(x => x.grossProfit);
        foreach (var item in list) item.profitContributionRate = totalProfit == 0 ? 0 : Math.Round(item.grossProfit / totalProfit, 4);
        return list.OrderByDescending(x => x.grossProfit).ToList();
    }

    public async Task<List<InventoryTurnoverDto>> GetInventoryTurnoverAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date; var end = endDate.Date.AddDays(1);
        var products = await _db.PRODUCTs.AsNoTracking().Select(x => new
        {
            x.PRODUCT_ID, x.PRODUCT_NAME, Ending = x.INVENTORies.Sum(i => (int?)i.CURRENT_STOCK) ?? 0
        }).ToListAsync();
        var changes = await _db.INVENTORY_RECORDs.AsNoTracking().Where(x => x.RECORD_TIME >= start && x.RECORD_TIME < end)
            .GroupBy(x => x.PRODUCT_ID).Select(x => new
            {
                ProductId = x.Key, Change = x.Sum(r => r.CHANGE_QTY),
                Sold = -x.Where(r => r.RECORD_TYPE == "销售").Sum(r => r.CHANGE_QTY)
            }).ToDictionaryAsync(x => x.ProductId);
        return products.Select(x =>
        {
            var flow = changes.GetValueOrDefault(x.PRODUCT_ID);
            var beginning = x.Ending - (flow?.Change ?? 0);
            var average = Math.Max(0, (beginning + x.Ending) / 2m);
            var sold = flow?.Sold ?? 0;
            return new InventoryTurnoverDto
            {
                productId = x.PRODUCT_ID, productName = x.PRODUCT_NAME, soldQuantity = sold,
                beginningStock = beginning, endingStock = x.Ending, averageStock = average,
                turnoverTimes = average > 0 ? Math.Round(sold / average, 4) : 0,
                stagnant = sold == 0 && x.Ending > 0
            };
        }).OrderBy(x => x.turnoverTimes).ToList();
    }

    public async Task<DailySettlementDto> GenerateDailySettlementAsync(DateTime date)
    {
        var day = date.Date;
        var shanghaiToday = ShanghaiNow().Date;
        if (day >= shanghaiToday)
            throw new ArgumentException("只能生成已经闭店的历史营业日日结");
        var existing = await _db.DAILY_SETTLEMENTs.FirstOrDefaultAsync(x => x.SETTLEMENT_DATE == day);
        if (existing is not null) return ToDailySettlementDto(existing);
        var end = day.AddDays(1);
        var sales = await _db.SALE_ORDERs.AsNoTracking()
            .Where(x => x.STATUS == "已完成" && x.SALE_DATE >= day && x.SALE_DATE < end).ToListAsync();
        var saleIds = sales.Select(x => x.SALE_ID).ToList();
        var pointConsumed = saleIds.Count == 0 ? 0 : -(await _db.POINT_RECORDs.AsNoTracking()
            .Where(x => saleIds.Contains(x.SALE_ID ?? 0) && x.CHANGE_TYPE == "抵现").SumAsync(x => (int?)x.CHANGE_POINTS) ?? 0);
        var pointDeduct = sales.Sum(x => x.POINT_DEDUCT ?? 0);
        var couponDeduct = sales.Sum(x => x.COUPON_DEDUCT ?? 0);
        var memberDiscount = sales.Sum(x => x.MEMBER_DISCOUNT ?? 0);
        var promotionDiscount = sales.Sum(x => x.PROMOTION_DISCOUNT ?? 0);
        // 退款以实际确认完成时间归属营业日，跨日退款不回改原销售日。
        var refundAmount = await _db.RETURN_ORDERs.AsNoTracking()
            .Where(x => x.STATUS == "已完成" && x.UPDATE_TIME >= day && x.UPDATE_TIME < end)
            .SumAsync(x => (decimal?)x.REFUND_AMOUNT) ?? 0;
        var totalSales = sales.Sum(x => x.PAID_AMOUNT ?? 0);
        var settlement = new DAILY_SETTLEMENT
        {
            SETTLEMENT_DATE = day, TOTAL_SALES = totalSales,
            REFUND_AMOUNT = refundAmount, NET_SALES = totalSales - refundAmount,
            // 日结不区分支付方式；旧字段保留为 0，仅用于兼容已有数据库结构和客户端。
            CASH_AMOUNT = 0, WECHAT_AMOUNT = 0, ALIPAY_AMOUNT = 0,
            PROMOTION_DISCOUNT = promotionDiscount, MEMBER_DISCOUNT = memberDiscount, COUPON_DEDUCT = couponDeduct,
            POINT_DEDUCT = pointDeduct, POINT_CONSUMED = pointConsumed,
            ORDER_COUNT = sales.Count, STATUS = "已生成", CREATE_TIME = ShanghaiNow()
        };
        _db.DAILY_SETTLEMENTs.Add(settlement);
        await _db.SaveChangesAsync();
        return ToDailySettlementDto(settlement);
    }

    public async Task<DailySettlementDto> GetDailySettlementAsync(DateTime date)
    {
        var record = await _db.DAILY_SETTLEMENTs.AsNoTracking().FirstOrDefaultAsync(x => x.SETTLEMENT_DATE == date.Date)
            ?? throw new KeyNotFoundException("当日尚未生成营业结转");
        return ToDailySettlementDto(record);
    }

    private static DailySettlementDto ToDailySettlementDto(DAILY_SETTLEMENT x) => new()
    {
        settlementId = x.SETTLEMENT_ID, settlementDate = x.SETTLEMENT_DATE, totalSales = x.TOTAL_SALES ?? 0,
        refundAmount = x.REFUND_AMOUNT ?? 0, netSales = x.NET_SALES ?? ((x.TOTAL_SALES ?? 0) - (x.REFUND_AMOUNT ?? 0)),
        cashAmount = x.CASH_AMOUNT ?? 0, wechatAmount = x.WECHAT_AMOUNT ?? 0, alipayAmount = x.ALIPAY_AMOUNT ?? 0,
        promotionDiscount = x.PROMOTION_DISCOUNT ?? 0, memberDiscount = x.MEMBER_DISCOUNT ?? 0,
        couponDeduct = x.COUPON_DEDUCT ?? 0, pointDeduct = x.POINT_DEDUCT ?? 0, pointConsumed = x.POINT_CONSUMED ?? 0,
        orderCount = x.ORDER_COUNT ?? 0, status = x.STATUS ?? string.Empty, createTime = x.CREATE_TIME
    };

    private static DateTime ShanghaiNow()
    {
        var zone = FindShanghaiTimeZone();
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
    }

    internal static TimeZoneInfo FindShanghaiTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"); }
    }
}
