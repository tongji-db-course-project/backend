using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 采购订单明细表
/// </summary>
public partial class PURCHASE_ORDER_DETAIL
{
    /// <summary>
    /// 明细编号
    /// </summary>
    public int PURCHASE_DETAIL_ID { get; set; }

    /// <summary>
    /// 采购单编号
    /// </summary>
    public int PURCHASE_ID { get; set; }

    /// <summary>
    /// 商品编号
    /// </summary>
    public int PRODUCT_ID { get; set; }

    /// <summary>
    /// 采购数量
    /// </summary>
    public int? PURCHASE_QUANTITY { get; set; }

    /// <summary>
    /// 采购单价
    /// </summary>
    public decimal? PURCHASE_PRICE { get; set; }

    public virtual PRODUCT PRODUCT { get; set; } = null!;

    public virtual PURCHASE_ORDER PURCHASE { get; set; } = null!;
}
