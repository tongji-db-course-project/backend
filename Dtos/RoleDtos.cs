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
    public string roleName { get; set; } = string.Empty;

    public string? roleDesc { get; set; }
}

public class AssignRoleMenusRequest
{
    public IEnumerable<int> menuIds { get; set; } = new List<int>();
}
