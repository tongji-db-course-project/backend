using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 采购退货单主表：处理向供应商退货的业务单据
/// </summary>
public partial class PURCHASE_RETURN_ORDER
{
    /// <summary>
    /// 采购退货单编号
    /// </summary>
    public int RETURN_ID { get; set; }

    /// <summary>
    /// 采购退货单号
    /// </summary>
    public string RETURN_NO { get; set; } = null!;

    /// <summary>
    /// 关联的原采购订单编号
    /// </summary>
    public int PURCHASE_ID { get; set; }

    /// <summary>
    /// 供应商编号
    /// </summary>
    public int SUPPLIER_ID { get; set; }

    /// <summary>
    /// 经办人编号
    /// </summary>
    public int OPERATOR_ID { get; set; }

    /// <summary>
    /// 退货日期
    /// </summary>
    public DateTime? RETURN_DATE { get; set; }

    /// <summary>
    /// 退货总金额
    /// </summary>
    public decimal TOTAL_AMOUNT { get; set; }

    /// <summary>
    /// 退货单状态
    /// </summary>
    public string? STATUS { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CREATE_TIME { get; set; }

    /// <summary>
    /// 最后状态变更时间
    /// </summary>
    public DateTime? UPDATE_TIME { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? REMARK { get; set; }

    public virtual SYS_USER OPERATOR { get; set; } = null!;

    public virtual PURCHASE_ORDER PURCHASE { get; set; } = null!;

    public virtual ICollection<PURCHASE_RETURN_ORDER_DETAIL> PURCHASE_RETURN_ORDER_DETAILs { get; set; } = new List<PURCHASE_RETURN_ORDER_DETAIL>();

    public virtual SUPPLIER SUPPLIER { get; set; } = null!;
}
