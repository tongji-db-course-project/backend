using System;
using System.Collections.Generic;

namespace backend.Models;

/// <summary>
/// 权限中间表：定义哪些角色可以访问哪些菜单
/// </summary>
public partial class SYS_ROLE_MENU
{
    public int ROLE_MENU_ID { get; set; }

    public int ROLE_ID { get; set; }

    public int MENU_ID { get; set; }

    public virtual SYS_MENU MENU { get; set; } = null!;

    public virtual SYS_ROLE ROLE { get; set; } = null!;
}
