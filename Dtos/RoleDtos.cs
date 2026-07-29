using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class RoleListItemDto
{
    public int roleId { get; set; }

    public string roleName { get; set; } = string.Empty;

    public string? roleDesc { get; set; }
}

public class RoleDetailDto : RoleListItemDto
{
}

public class UpdateRoleRequest
{
    [Required(ErrorMessage = "角色名称不能为空")]
    [StringLength(50, ErrorMessage = "角色名称不能超过50个字符")]
    public string roleName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "角色说明不能超过200个字符")]
    public string? roleDesc { get; set; }
}

public class AssignRoleMenusRequest
{
    [Required(ErrorMessage = "菜单编号列表不能为空")]
    public IEnumerable<int> menuIds { get; set; } = new List<int>();
}
