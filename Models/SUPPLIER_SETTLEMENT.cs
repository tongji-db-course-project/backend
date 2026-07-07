using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 供应商结算表：管理与供应商的财务结账情况
/// </summary>
public partial class SUPPLIER_SETTLEMENT
{
    public int SETTLEMENT_ID { get; set; }

    public int SUPPLIER_ID { get; set; }

    public int PURCHASE_ID { get; set; }

    public DateTime? SETTLEMENT_DATE { get; set; }

    public decimal SETTLEMENT_AMOUNT { get; set; }

    public decimal? PAID_AMOUNT { get; set; }

    public decimal UNPAID_AMOUNT { get; set; }

    public string STATUS { get; set; } = null!;

    public string? REMARK { get; set; }

    public virtual PURCHASE_ORDER PURCHASE { get; set; } = null!;

    public virtual SUPPLIER SUPPLIER { get; set; } = null!;
}
