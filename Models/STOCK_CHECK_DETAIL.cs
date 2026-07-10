using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 库存盘点明细表：逐商品比对系统库存与实际库存
/// </summary>
public partial class STOCK_CHECK_DETAIL
{
    public int DETAIL_ID { get; set; }

    /// <summary>
    /// 盘点单编号
    /// </summary>
    public int CHECK_ID { get; set; }

    /// <summary>
    /// 商品编号
    /// </summary>
    public int PRODUCT_ID { get; set; }

    /// <summary>
    /// 系统库存数量
    /// </summary>
    public int SYSTEM_QTY { get; set; }

    /// <summary>
    /// 实际盘点数量
    /// </summary>
    public int ACTUAL_QTY { get; set; }

    /// <summary>
    /// 差异数量（实际-系统，正=盘盈，负=盘亏）
    /// </summary>
    public int DIFFERENCE_QTY { get; set; }

    /// <summary>
    /// 调整单价
    /// </summary>
    public decimal? ADJUST_PRICE { get; set; }

    /// <summary>
    /// 损益金额
    /// </summary>
    public decimal? ADJUST_AMOUNT { get; set; }

    public virtual STOCK_CHECK_ORDER CHECK { get; set; } = null!;

    public virtual PRODUCT PRODUCT { get; set; } = null!;
}
