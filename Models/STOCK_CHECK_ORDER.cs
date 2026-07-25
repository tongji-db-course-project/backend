using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 库存盘点单主表：记录盘点任务与执行状态
/// </summary>
public partial class STOCK_CHECK_ORDER
{
    public int CHECK_ID { get; set; }

    /// <summary>
    /// 盘点单号
    /// </summary>
    public string CHECK_NO { get; set; } = null!;

    /// <summary>
    /// 盘点仓库编号
    /// </summary>
    public int WAREHOUSE_ID { get; set; }

    /// <summary>
    /// 盘点类型：定期盘点/动态盘点
    /// </summary>
    public string? CHECK_TYPE { get; set; }

    /// <summary>
    /// 盘点单状态
    /// </summary>
    public string? STATUS { get; set; }

    /// <summary>
    /// 盘点操作人编号
    /// </summary>
    public int OPERATOR_ID { get; set; }

    /// <summary>
    /// 盘点日期
    /// </summary>
    public DateTime? CHECK_DATE { get; set; }

    /// <summary>
    /// 盘点完成日期
    /// </summary>
    public DateTime? COMPLETE_DATE { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? REMARK { get; set; }

    public virtual SYS_USER OPERATOR { get; set; } = null!;

    public virtual ICollection<STOCK_CHECK_DETAIL> STOCK_CHECK_DETAILs { get; set; } = new List<STOCK_CHECK_DETAIL>();

    public virtual WAREHOUSE WAREHOUSE { get; set; } = null!;
}
