using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 积分规则配置表：定义积分获取与抵扣比例
/// </summary>
public partial class POINT_CONFIG
{
    public int CONFIG_ID { get; set; }

    /// <summary>
    /// 积分获取率（如0.01=消费100元得1积分）
    /// </summary>
    public decimal EARN_RATE { get; set; }

    /// <summary>
    /// 积分抵扣率（如0.01=1积分抵0.01元）
    /// </summary>
    public decimal REDEEM_RATE { get; set; }

    /// <summary>
    /// 最低使用积分数量
    /// </summary>
    public int? REDEEM_MIN { get; set; }

    /// <summary>
    /// 单笔订单积分抵扣比例上限（如0.5=最多抵50%）
    /// </summary>
    public decimal? REDEEM_MAX_RATE { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public string? STATUS { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime? UPDATE_TIME { get; set; }
}
