namespace backend.Dtos;

/// <summary>
/// 商品毛利分析返回结构
/// </summary>
public class ProfitStatistics
{
    /// <summary>
    /// 总销售金额
    /// </summary>
    public decimal TotalSaleAmount { get; set; }

    /// <summary>
    /// 总采购成本
    /// </summary>
    public decimal TotalPurchaseCost { get; set; }

    /// <summary>
    /// 毛利润
    /// </summary>
    public decimal GrossProfit { get; set; }

    /// <summary>
    /// 毛利率
    /// </summary>
    public decimal GrossProfitRate { get; set; }
}
