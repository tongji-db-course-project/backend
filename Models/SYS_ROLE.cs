using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 角色表
/// </summary>
public partial class SYS_ROLE
{
    /// <summary>
    /// 角色编号
    /// </summary>
    public int ROLE_ID { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    public string ROLE_NAME { get; set; } = null!;

    /// <summary>
    /// 角色说明
    /// </summary>
    public string? ROLE_DESC { get; set; }

    public virtual ICollection<SYS_ROLE_MENU> SYS_ROLE_MENUs { get; set; } = new List<SYS_ROLE_MENU>();

    public virtual ICollection<SYS_USER> SYS_USERs { get; set; } = new List<SYS_USER>();
}
