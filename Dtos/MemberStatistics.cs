namespace backend.Dtos;

/// <summary>
/// 会员消费统计返回结构
/// </summary>
public class MemberStatistics
{
    /// <summary>
    /// 会员总数
    /// </summary>
    public long MemberCount { get; set; }

    /// <summary>
    /// 活跃会员数
    /// </summary>
    public long ActiveMemberCount { get; set; }

    /// <summary>
    /// 会员消费总额
    /// </summary>
    public double MemberSaleAmount { get; set; }

    /// <summary>
    /// 会员平均消费金额
    /// </summary>
    public double AverageSaleAmount { get; set; }
}
