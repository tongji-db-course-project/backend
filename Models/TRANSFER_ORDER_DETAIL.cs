using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 调拨单明细表
/// </summary>
public partial class TRANSFER_ORDER_DETAIL
{
    public int DETAIL_ID { get; set; }

    /// <summary>
    /// 调拨单编号
    /// </summary>
    public int TRANSFER_ID { get; set; }

    /// <summary>
    /// 商品编号
    /// </summary>
    public int PRODUCT_ID { get; set; }

    /// <summary>
    /// 调拨数量
    /// </summary>
    public int QUANTITY { get; set; }

    public virtual PRODUCT PRODUCT { get; set; } = null!;

    public virtual TRANSFER_ORDER TRANSFER { get; set; } = null!;
}
