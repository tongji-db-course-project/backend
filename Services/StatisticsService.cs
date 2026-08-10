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
            .Where(o => o.SALE_DATE >= start && o.SALE_DATE <= end)
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
            .Where(r => r.RETURN_DATE >= start && r.RETURN_DATE <= end)
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
            .Where(o => o.SALE_DATE >= start && o.SALE_DATE <= end)
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
            .Where(r => r.RETURN_DATE >= start && r.RETURN_DATE <= end)
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
            where s.SALE_DATE >= start && s.SALE_DATE <= end
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
            where s.SALE_DATE >= start && s.SALE_DATE <= end
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
            .Where(o => o.MEMBER_ID != null);

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
}
