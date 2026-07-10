using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 订单状态流转日志表：记录采购/销售/退货单的每一次状态变更
/// </summary>
public partial class ORDER_STATUS_LOG
{
    public int LOG_ID { get; set; }

    /// <summary>
    /// 订单类型：采购单/销售单/退货单
    /// </summary>
    public string ORDER_TYPE { get; set; } = null!;

    /// <summary>
    /// 对应订单主表的主键
    /// </summary>
    public int ORDER_ID { get; set; }

    /// <summary>
    /// 变更前状态（首次创建时为NULL）
    /// </summary>
    public string? OLD_STATUS { get; set; }

    /// <summary>
    /// 变更后状态
    /// </summary>
    public string NEW_STATUS { get; set; } = null!;

    /// <summary>
    /// 操作人编号
    /// </summary>
    public int? OPERATOR_ID { get; set; }

    /// <summary>
    /// 状态变更时间
    /// </summary>
    public DateTime? CHANGE_TIME { get; set; }

    /// <summary>
    /// 备注（如审批意见）
    /// </summary>
    public string? REMARK { get; set; }
}
