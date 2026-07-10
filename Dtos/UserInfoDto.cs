namespace backend.Dtos;

public class UserInfoDto
{
    public int userId { get; set; }

    public string username { get; set; } = string.Empty;

    public string? realName { get; set; }

    public int? roleId { get; set; }

    public string? roleName { get; set; }

    public string? status { get; set; }
}
