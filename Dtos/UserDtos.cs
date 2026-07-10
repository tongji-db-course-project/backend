namespace backend.Dtos;

public class UserListItemDto
{
    public int userId { get; set; }

    public int? roleId { get; set; }

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

    public string username { get; set; } = string.Empty;

    public string password { get; set; } = string.Empty;

    public string? realName { get; set; }

    public string? gender { get; set; }

    public string? phone { get; set; }

    public string? status { get; set; }
}

public class UpdateUserRequest
{
    public int? roleId { get; set; }

    public string? realName { get; set; }

    public string? gender { get; set; }

    public string? phone { get; set; }

    public string? status { get; set; }
}

public class ChangeUserStatusRequest
{
    public string status { get; set; } = string.Empty;
}
