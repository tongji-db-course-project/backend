namespace backend.Dtos;

/// <summary>
/// 月销售统计返回结构
/// </summary>
public class MonthlySalesStatistics
{
    /// <summary>
    /// 统计月份
    /// </summary>
    public string Month { get; set; } = string.Empty;

    /// <summary>
    /// 订单数量
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// 销售总额
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 实收金额
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// 退款金额
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// 净销售额
    /// </summary>
    public decimal NetAmount { get; set; }
}
