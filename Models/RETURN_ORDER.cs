using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class RETURN_ORDER
{
    /// <summary>
    /// 退货单编号
    /// </summary>
    public int RETURN_ID { get; set; }

    /// <summary>
    /// 退货单号
    /// </summary>
    public string RETURN_NO { get; set; } = null!;

    /// <summary>
    /// 对应销售单编号
    /// </summary>
    public int SALE_ID { get; set; }

    /// <summary>
    /// 会员编号
    /// </summary>
    public int? MEMBER_ID { get; set; }

    /// <summary>
    /// 经办人编号
    /// </summary>
    public int OPERATOR_ID { get; set; }

    /// <summary>
    /// 退货日期
    /// </summary>
    public DateTime RETURN_DATE { get; set; }

    /// <summary>
    /// 退款金额
    /// </summary>
    public decimal REFUND_AMOUNT { get; set; }

    /// <summary>
    /// 退货状态
    /// </summary>
    public string STATUS { get; set; } = null!;

    /// <summary>
    /// 备注
    /// </summary>
    public string? REMARK { get; set; }

    public virtual MEMBER? MEMBER { get; set; }

    public virtual SYS_USER OPERATOR { get; set; } = null!;

    public virtual ICollection<RETURN_ORDER_DETAIL> RETURN_ORDER_DETAILs { get; set; } = new List<RETURN_ORDER_DETAIL>();

    public virtual SALE_ORDER SALE { get; set; } = null!;
}
