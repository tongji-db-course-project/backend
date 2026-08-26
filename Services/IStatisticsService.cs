using backend.Dtos;

namespace backend.Services;

/// <summary>
/// 统计业务接口
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// 按日期统计销售数据（按天分组，返回每天的统计）
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>每日销售统计列表</returns>
    Task<List<SalesStatistics>> GetDailySalesStatisticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 按月份统计销售数据（按月分组，返回每月的统计）
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>每月销售统计列表</returns>
    Task<List<MonthlySalesStatistics>> GetMonthlySalesStatisticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 查询商品销量排行
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>商品销量排行列表</returns>
    Task<List<ProductRank>> GetProductRankAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 商品毛利分析
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>毛利统计数据</returns>
    Task<ProfitStatistics> GetProfitStatisticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 库存统计 - 统计库存总量和低库存数量
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>库存统计数据</returns>
    Task<InventoryStatistics> GetInventoryStatisticsAsync(DateTime? startDate, DateTime? endDate);

    /// <summary>
    /// 会员消费统计
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>会员消费统计数据</returns>
    Task<MemberStatistics> GetMemberStatisticsAsync(DateTime? startDate, DateTime? endDate);

    Task<List<ProductProfitRankDto>> GetProductProfitRankAsync(DateTime startDate, DateTime endDate);
    Task<List<InventoryTurnoverDto>> GetInventoryTurnoverAsync(DateTime startDate, DateTime endDate);
    Task<DailySettlementDto> GenerateDailySettlementAsync(DateTime date);
    Task<DailySettlementDto> GetDailySettlementAsync(DateTime date);
}
