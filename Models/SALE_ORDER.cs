using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 销售订单表
/// </summary>
public partial class SALE_ORDER
{
    /// <summary>
    /// 销售单编号
    /// </summary>
    public int SALE_ID { get; set; }

    /// <summary>
    /// 销售单号
    /// </summary>
    public string SALE_NO { get; set; } = null!;

    /// <summary>
    /// 会员编号
    /// </summary>
    public int? MEMBER_ID { get; set; }

    /// <summary>
    /// 收银员编号
    /// </summary>
    public int USER_ID { get; set; }

    /// <summary>
    /// 销售日期
    /// </summary>
    public DateTime? SALE_DATE { get; set; }

    /// <summary>
    /// 原始总金额
    /// </summary>
    public decimal? TOTAL_AMOUNT { get; set; }

    /// <summary>
    /// 优惠金额
    /// </summary>
    public decimal? DISCOUNT_AMOUNT { get; set; }

    /// <summary>
    /// 实付金额
    /// </summary>
    public decimal? PAID_AMOUNT { get; set; }

    /// <summary>
    /// 支付方式
    /// </summary>
    public string? PAY_TYPE { get; set; }

    /// <summary>
    /// 销售单状态
    /// </summary>
    public string? STATUS { get; set; }

    public virtual MEMBER? MEMBER { get; set; }

    public virtual ICollection<POINT_RECORD> POINT_RECORDs { get; set; } = new List<POINT_RECORD>();

    public virtual ICollection<RETURN_ORDER> RETURN_ORDERs { get; set; } = new List<RETURN_ORDER>();

    public virtual ICollection<SALE_ORDER_DETAIL> SALE_ORDER_DETAILs { get; set; } = new List<SALE_ORDER_DETAIL>();

    public virtual SYS_USER USER { get; set; } = null!;
}
