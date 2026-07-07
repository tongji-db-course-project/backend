using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 销售单明细表
/// </summary>
public partial class SALE_ORDER_DETAIL
{
    /// <summary>
    /// 明细编号
    /// </summary>
    public int SALE_DETAIL_ID { get; set; }

    /// <summary>
    /// 销售单编号
    /// </summary>
    public int SALE_ID { get; set; }

    /// <summary>
    /// 商品编号
    /// </summary>
    public int PRODUCT_ID { get; set; }

    /// <summary>
    /// 销售数量
    /// </summary>
    public int? SALE_QUANTITY { get; set; }

    /// <summary>
    /// 销售单价
    /// </summary>
    public decimal? SALE_PRICE { get; set; }

    public virtual PRODUCT PRODUCT { get; set; } = null!;

    public virtual SALE_ORDER SALE { get; set; } = null!;
}
