using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 菜单功能表
/// </summary>
public partial class SYS_MENU
{
    /// <summary>
    /// 菜单编号
    /// </summary>
    public int MENU_ID { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    public string MENU_NAME { get; set; } = null!;

    /// <summary>
    /// 菜单路径
    /// </summary>
    public string? MENU_URL { get; set; }

    /// <summary>
    /// 上级菜单编号
    /// </summary>
    public int? PARENT_ID { get; set; }

    /// <summary>
    /// 菜单顺序
    /// </summary>
    public short? MENU_ORDER { get; set; }

    public virtual ICollection<SYS_ROLE_MENU> SYS_ROLE_MENUs { get; set; } = new List<SYS_ROLE_MENU>();
}
