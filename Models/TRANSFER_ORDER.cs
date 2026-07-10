using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 调拨单主表：处理商品在不同仓库/门店之间的转移
/// </summary>
public partial class TRANSFER_ORDER
{
    public int TRANSFER_ID { get; set; }

    /// <summary>
    /// 调拨单号
    /// </summary>
    public string TRANSFER_NO { get; set; } = null!;

    /// <summary>
    /// 源仓库编号
    /// </summary>
    public int FROM_WAREHOUSE { get; set; }

    /// <summary>
    /// 目标仓库编号
    /// </summary>
    public int TO_WAREHOUSE { get; set; }

    /// <summary>
    /// 调拨单状态
    /// </summary>
    public string? STATUS { get; set; }

    /// <summary>
    /// 申请人编号
    /// </summary>
    public int APPLICANT_ID { get; set; }

    /// <summary>
    /// 审批人编号
    /// </summary>
    public int? APPROVER_ID { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CREATE_TIME { get; set; }

    /// <summary>
    /// 调拨完成时间
    /// </summary>
    public DateTime? COMPLETE_TIME { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? REMARK { get; set; }

    public virtual SYS_USER APPLICANT { get; set; } = null!;

    public virtual SYS_USER? APPROVER { get; set; }

    public virtual WAREHOUSE FROM_WAREHOUSENavigation { get; set; } = null!;

    public virtual WAREHOUSE TO_WAREHOUSENavigation { get; set; } = null!;

    public virtual ICollection<TRANSFER_ORDER_DETAIL> TRANSFER_ORDER_DETAILs { get; set; } = new List<TRANSFER_ORDER_DETAIL>();
}
