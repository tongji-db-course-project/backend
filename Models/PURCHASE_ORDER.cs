using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 采购订单主表
/// </summary>
public partial class PURCHASE_ORDER
{
    /// <summary>
    /// 采购订单唯一标识 (主键)
    /// </summary>
    public int ORDER_ID { get; set; }

    /// <summary>
    /// 采购单据编码 (系统生成, 唯一)
    /// </summary>
    public string ORDER_CODE { get; set; } = null!;

    /// <summary>
    /// 供应商ID (关联supplier表)
    /// </summary>
    public int? SUPPLIER_ID { get; set; }

    public DateTime? PURCHASE_DATE { get; set; }

    public decimal? TOTAL_AMOUNT { get; set; }

    /// <summary>
    /// 申请用户ID (关联sys_user表)
    /// </summary>
    public int? APPLICANT_ID { get; set; }

    /// <summary>
    /// 审批用户ID (关联sys_user表)
    /// </summary>
    public int? APPROVER_ID { get; set; }

    /// <summary>
    /// 单据状态
    /// </summary>
    public string? STATUS { get; set; }

    public virtual SYS_USER? APPLICANT { get; set; }

    public virtual SYS_USER? APPROVER { get; set; }

    public virtual ICollection<PURCHASE_ORDER_DETAIL> PURCHASE_ORDER_DETAILs { get; set; } = new List<PURCHASE_ORDER_DETAIL>();

    public virtual SUPPLIER? SUPPLIER { get; set; }

    public virtual ICollection<SUPPLIER_SETTLEMENT> SUPPLIER_SETTLEMENTs { get; set; } = new List<SUPPLIER_SETTLEMENT>();
}
