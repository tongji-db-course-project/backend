using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 优惠券模板表：定义一种优惠券的规则与发放总量
/// </summary>
public partial class COUPON_TEMPLATE
{
    public int TEMPLATE_ID { get; set; }

    /// <summary>
    /// 优惠券名称
    /// </summary>
    public string COUPON_NAME { get; set; } = null!;

    /// <summary>
    /// 券类型：满减券/折扣券/现金券
    /// </summary>
    public string COUPON_TYPE { get; set; } = null!;

    /// <summary>
    /// 使用门槛金额
    /// </summary>
    public decimal? MIN_AMOUNT { get; set; }

    /// <summary>
    /// 面值（满减券=减X元，折扣券=0.85折，现金券=抵扣X元）
    /// </summary>
    public decimal FACE_VALUE { get; set; }

    /// <summary>
    /// 折扣券最高抵扣金额
    /// </summary>
    public decimal? MAX_DISCOUNT { get; set; }

    /// <summary>
    /// 领券后有效天数
    /// </summary>
    public short VALID_DAYS { get; set; }

    /// <summary>
    /// 发放总量
    /// </summary>
    public int? TOTAL_COUNT { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public string? STATUS { get; set; }

    public DateTime? CREATE_TIME { get; set; }

    public virtual ICollection<MEMBER_COUPON> MEMBER_COUPONs { get; set; } = new List<MEMBER_COUPON>();
}
