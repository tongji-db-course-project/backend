using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class INVENTORY
{
    /// <summary>
    /// 库存编号
    /// </summary>
    public int INVENTORY_ID { get; set; }

    /// <summary>
    /// 商品编号，关联商品表
    /// </summary>
    public int PRODUCT_ID { get; set; }

    /// <summary>
    /// 当前库存量
    /// </summary>
    public int CURRENT_STOCK { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LAST_UPDATE_TIME { get; set; }

    public virtual PRODUCT PRODUCT { get; set; } = null!;
}
