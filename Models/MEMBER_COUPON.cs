using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 会员持有优惠券表：记录每张已发放优惠券的归属与使用状态
/// </summary>
public partial class MEMBER_COUPON
{
    public int COUPON_ID { get; set; }

    /// <summary>
    /// 优惠券模板编号
    /// </summary>
    public int TEMPLATE_ID { get; set; }

    /// <summary>
    /// 持有会员编号
    /// </summary>
    public int MEMBER_ID { get; set; }

    /// <summary>
    /// 券状态：未使用/已使用/已过期
    /// </summary>
    public string? STATUS { get; set; }

    /// <summary>
    /// 领取时间
    /// </summary>
    public DateTime? RECEIVE_TIME { get; set; }

    /// <summary>
    /// 使用时间
    /// </summary>
    public DateTime? USE_TIME { get; set; }

    /// <summary>
    /// 使用该券的销售单编号
    /// </summary>
    public int? SALE_ID { get; set; }

    public virtual MEMBER MEMBER { get; set; } = null!;

    public virtual SALE_ORDER? SALE { get; set; }

    public virtual COUPON_TEMPLATE TEMPLATE { get; set; } = null!;
}
