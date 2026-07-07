using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 会员信息表
/// </summary>
public partial class MEMBER
{
    /// <summary>
    /// 会员唯一标识 (主键)
    /// </summary>
    public int MEMBER_ID { get; set; }

    public string MEMBER_NAME { get; set; } = null!;

    public string? GENDER { get; set; }

    /// <summary>
    /// 会员手机号 (唯一约束)
    /// </summary>
    public string PHONE { get; set; } = null!;

    public DateTime? BIRTHDAY { get; set; }

    /// <summary>
    /// 当前剩余积分
    /// </summary>
    public int? POINTS { get; set; }

    public decimal? TOTAL_AMOUNT { get; set; }

    /// <summary>
    /// 会员等级（硬编码文本）
    /// </summary>
    public string? LEVEL_NAME { get; set; }

    public DateTime? CREATE_TIME { get; set; }

    public virtual ICollection<POINT_RECORD> POINT_RECORDs { get; set; } = new List<POINT_RECORD>();

    public virtual ICollection<RETURN_ORDER> RETURN_ORDERs { get; set; } = new List<RETURN_ORDER>();

    public virtual ICollection<SALE_ORDER> SALE_ORDERs { get; set; } = new List<SALE_ORDER>();
}
