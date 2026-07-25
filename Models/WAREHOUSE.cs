using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 仓库/门店表：管理系统中的物理仓储节点
/// </summary>
public partial class WAREHOUSE
{
    /// <summary>
    /// 仓库编号
    /// </summary>
    public int WAREHOUSE_ID { get; set; }

    /// <summary>
    /// 仓库/门店名称
    /// </summary>
    public string WAREHOUSE_NAME { get; set; } = null!;

    /// <summary>
    /// 仓库地址
    /// </summary>
    public string? ADDRESS { get; set; }

    /// <summary>
    /// 状态（启用/禁用）
    /// </summary>
    public string? STATUS { get; set; }

    public DateTime? CREATE_TIME { get; set; }

    public virtual ICollection<INVENTORY> INVENTORies { get; set; } = new List<INVENTORY>();

    public virtual ICollection<STOCK_CHECK_ORDER> STOCK_CHECK_ORDERs { get; set; } = new List<STOCK_CHECK_ORDER>();

    public virtual ICollection<TRANSFER_ORDER> TRANSFER_ORDERFROM_WAREHOUSENavigations { get; set; } = new List<TRANSFER_ORDER>();

    public virtual ICollection<TRANSFER_ORDER> TRANSFER_ORDERTO_WAREHOUSENavigations { get; set; } = new List<TRANSFER_ORDER>();
}
