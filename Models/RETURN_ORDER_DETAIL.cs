using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 退货单明细表：记录每一笔退货业务中包含的具体商品、数量及退款单价
/// </summary>
public partial class RETURN_ORDER_DETAIL
{
    public int RETURN_DETAIL_ID { get; set; }

    public int RETURN_ID { get; set; }

    public int PRODUCT_ID { get; set; }

    public int QUANTITY { get; set; }

    /// <summary>
    /// 退货单价
    /// </summary>
    public decimal REFUND_PRICE { get; set; }

    /// <summary>
    /// 该笔商品退款总计金额
    /// </summary>
    public decimal SUBTOTAL { get; set; }

    public virtual PRODUCT PRODUCT { get; set; } = null!;

    public virtual RETURN_ORDER RETURN { get; set; } = null!;
}
