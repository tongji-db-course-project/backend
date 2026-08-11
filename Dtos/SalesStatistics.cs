namespace backend.Dtos;

/// <summary>
/// 销售统计返回结构
/// </summary>
public class SalesStatistics
{
    /// <summary>
    /// 统计日期
    /// </summary>
    public string StatDate { get; set; } = string.Empty;

    /// <summary>
    /// 订单数量
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// 总金额
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 实付金额
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// 退款金额
    /// </summary>
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// 净金额（实付-退款）
    /// </summary>
    public decimal NetAmount { get; set; }
}
