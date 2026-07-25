using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 供应商信息表
/// </summary>
public partial class SUPPLIER
{
    /// <summary>
    /// 供应商唯一标识 (主键)
    /// </summary>
    public int SUPPLIER_ID { get; set; }

    public string SUPPLIER_NAME { get; set; } = null!;

    public string? CONTACT_NAME { get; set; }

    public string? PHONE { get; set; }

    public string? EMAIL { get; set; }

    public string? ADDRESS { get; set; }

    /// <summary>
    /// 供应商信誉等级
    /// </summary>
    public string? CREDIT_LEVEL { get; set; }

    /// <summary>
    /// 约定结算周期（天数）
    /// </summary>
    public short? PAYMENT_CYCLE { get; set; }

    /// <summary>
    /// 最小起订量
    /// </summary>
    public int? MIN_ORDER_QTY { get; set; }

    /// <summary>
    /// 开户银行名称
    /// </summary>
    public string? BANK_NAME { get; set; }

    /// <summary>
    /// 银行账号
    /// </summary>
    public string? BANK_ACCOUNT { get; set; }

    /// <summary>
    /// 供应商状态
    /// </summary>
    public string? STATUS { get; set; }

    public virtual ICollection<PRODUCT> PRODUCTs { get; set; } = new List<PRODUCT>();

    public virtual ICollection<PURCHASE_ORDER> PURCHASE_ORDERs { get; set; } = new List<PURCHASE_ORDER>();

    public virtual ICollection<PURCHASE_RETURN_ORDER> PURCHASE_RETURN_ORDERs { get; set; } = new List<PURCHASE_RETURN_ORDER>();

    public virtual ICollection<SUPPLIER_SETTLEMENT> SUPPLIER_SETTLEMENTs { get; set; } = new List<SUPPLIER_SETTLEMENT>();
}
