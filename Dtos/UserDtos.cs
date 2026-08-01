using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class UserListItemDto
{
    public int userId { get; set; }

    public int? roleId { get; set; }

    public string? roleName { get; set; }

    public string username { get; set; } = string.Empty;

    public string? realName { get; set; }

    public string? gender { get; set; }

    public string? phone { get; set; }

    public string? status { get; set; }

    public DateTime? createTime { get; set; }
}

public class UserDetailDto : UserListItemDto
{
}

public class CreateUserRequest
{
    public int? roleId { get; set; }

    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, ErrorMessage = "用户名不能超过50个字符")]
    public string username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须为6到100个字符")]
    public string password { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "真实姓名不能超过50个字符")]
    public string? realName { get; set; }

    [StringLength(10, ErrorMessage = "性别不能超过10个字符")]
    public string? gender { get; set; }

    [StringLength(20, ErrorMessage = "联系电话不能超过20个字符")]
    public string? phone { get; set; }

    [RegularExpression("^(启用|禁用)$", ErrorMessage = "状态只能是：启用、禁用")]
    public string? status { get; set; }
}

public class UpdateUserRequest
{
    public int? roleId { get; set; }

    [StringLength(50, ErrorMessage = "真实姓名不能超过50个字符")]
    public string? realName { get; set; }

    [StringLength(10, ErrorMessage = "性别不能超过10个字符")]
    public string? gender { get; set; }

    [StringLength(20, ErrorMessage = "联系电话不能超过20个字符")]
    public string? phone { get; set; }

    [RegularExpression("^(启用|禁用)$", ErrorMessage = "状态只能是：启用、禁用")]
    public string? status { get; set; }
}

public class ChangeUserStatusRequest
{
    [Required(ErrorMessage = "状态不能为空")]
    [RegularExpression("^(启用|禁用)$", ErrorMessage = "状态只能是：启用、禁用")]
    public string status { get; set; } = string.Empty;
}
