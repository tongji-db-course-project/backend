using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 商品基础资料表
/// </summary>
public partial class PRODUCT
{
    public int PRODUCT_ID { get; set; }

    public int CATEGORY_ID { get; set; }

    public int SUPPLIER_ID { get; set; }

    public string PRODUCT_NAME { get; set; } = null!;

    public string? BARCODE { get; set; }

    public string? SPECIFICATION { get; set; }

    public decimal? PURCHASE_PRICE { get; set; }

    public decimal? SALE_PRICE { get; set; }

    public int? STOCK_WARNING { get; set; }

    public string? UNIT { get; set; }

    public string? STATUS { get; set; }

    public virtual PRODUCT_CATEGORY CATEGORY { get; set; } = null!;

    public virtual ICollection<INVENTORY_RECORD> INVENTORY_RECORDs { get; set; } = new List<INVENTORY_RECORD>();

    public virtual ICollection<INVENTORY> INVENTORies { get; set; } = new List<INVENTORY>();

    public virtual ICollection<PURCHASE_ORDER_DETAIL> PURCHASE_ORDER_DETAILs { get; set; } = new List<PURCHASE_ORDER_DETAIL>();

    public virtual ICollection<RETURN_ORDER_DETAIL> RETURN_ORDER_DETAILs { get; set; } = new List<RETURN_ORDER_DETAIL>();

    public virtual ICollection<SALE_ORDER_DETAIL> SALE_ORDER_DETAILs { get; set; } = new List<SALE_ORDER_DETAIL>();

    public virtual SUPPLIER SUPPLIER { get; set; } = null!;
}
