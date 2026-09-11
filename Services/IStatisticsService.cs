using backend.Dtos;

namespace backend.Services;

/// <summary>
/// 统计业务接口
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// 按日期统计销售数据
    /// </summary>
    Task<List<SalesStatistics>> GetDailySalesStatisticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 按月份统计销售数据
    /// </summary>
    Task<List<MonthlySalesStatistics>> GetMonthlySalesStatisticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 查询商品销量排行
    /// </summary>
    Task<List<ProductRank>> GetProductRankAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 商品毛利分析
    /// </summary>
    Task<ProfitStatistics> GetProfitStatisticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 库存统计
    /// </summary>
    Task<InventoryStatistics> GetInventoryStatisticsAsync(DateTime? startDate, DateTime? endDate);

    /// <summary>
    /// 会员消费统计
    /// </summary>
    Task<MemberStatistics> GetMemberStatisticsAsync(DateTime? startDate, DateTime? endDate);

    Task<List<ProductProfitRankDto>> GetProductProfitRankAsync(DateTime startDate, DateTime endDate);
    Task<List<InventoryTurnoverDto>> GetInventoryTurnoverAsync(DateTime startDate, DateTime endDate);
    Task<DailySettlementDto> GenerateDailySettlementAsync(DateTime date);
    Task<DailySettlementDto> GetDailySettlementAsync(DateTime date);
}
