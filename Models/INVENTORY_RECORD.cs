using System;
using System.Collections.Generic;

namespace backend.Models;

public partial class INVENTORY_RECORD
{
    /// <summary>
    /// 流水编号
    /// </summary>
    public int RECORD_ID { get; set; }

    /// <summary>
    /// 商品编号
    /// </summary>
    public int PRODUCT_ID { get; set; }

    /// <summary>
    /// 流水类型(入库/销售/退货/盘点)
    /// </summary>
    public string RECORD_TYPE { get; set; } = null!;

    /// <summary>
    /// 来源单号
    /// </summary>
    public string? SOURCE_NO { get; set; }

    /// <summary>
    /// 变动数量
    /// </summary>
    public int CHANGE_QTY { get; set; }

    /// <summary>
    /// 变动后库存
    /// </summary>
    public int REMAIN_QTY { get; set; }

    /// <summary>
    /// 操作人编号
    /// </summary>
    public int OPERATOR_ID { get; set; }

    /// <summary>
    /// 记录时间
    /// </summary>
    public DateTime RECORD_TIME { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? REMARK { get; set; }

    public virtual SYS_USER OPERATOR { get; set; } = null!;

    public virtual PRODUCT PRODUCT { get; set; } = null!;
}
