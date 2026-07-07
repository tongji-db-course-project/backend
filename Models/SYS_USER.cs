using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 用户表
/// </summary>
public partial class SYS_USER
{
    /// <summary>
    /// 用户编号
    /// </summary>
    public int USER_ID { get; set; }

    /// <summary>
    /// 角色编号
    /// </summary>
    public int? ROLE_ID { get; set; }

    /// <summary>
    /// 登录账号
    /// </summary>
    public string USERNAME { get; set; } = null!;

    /// <summary>
    /// 登录密码
    /// </summary>
    public string PASSWORD { get; set; } = null!;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? REAL_NAME { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public string? GENDER { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? PHONE { get; set; }

    /// <summary>
    /// 用户状态
    /// </summary>
    public string? STATUS { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CREATE_TIME { get; set; }

    public virtual ICollection<INVENTORY_RECORD> INVENTORY_RECORDs { get; set; } = new List<INVENTORY_RECORD>();

    public virtual ICollection<PURCHASE_ORDER> PURCHASE_ORDERAPPLICANTs { get; set; } = new List<PURCHASE_ORDER>();

    public virtual ICollection<PURCHASE_ORDER> PURCHASE_ORDERAPPROVERs { get; set; } = new List<PURCHASE_ORDER>();

    public virtual ICollection<RETURN_ORDER> RETURN_ORDERs { get; set; } = new List<RETURN_ORDER>();

    public virtual SYS_ROLE? ROLE { get; set; }

    public virtual ICollection<SALE_ORDER> SALE_ORDERs { get; set; } = new List<SALE_ORDER>();
}
