using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 采购退货单明细表
/// </summary>
public partial class PURCHASE_RETURN_ORDER_DETAIL
{
    public int DETAIL_ID { get; set; }

    /// <summary>
    /// 采购退货单编号
    /// </summary>
    public int RETURN_ID { get; set; }

    /// <summary>
    /// 商品编号
    /// </summary>
    public int PRODUCT_ID { get; set; }

    /// <summary>
    /// 退货数量
    /// </summary>
    public int QUANTITY { get; set; }

    /// <summary>
    /// 退货单价
    /// </summary>
    public decimal RETURN_PRICE { get; set; }

    /// <summary>
    /// 该行退款小计
    /// </summary>
    public decimal SUBTOTAL { get; set; }

    public virtual PRODUCT PRODUCT { get; set; } = null!;

    public virtual PURCHASE_RETURN_ORDER RETURN { get; set; } = null!;
}
