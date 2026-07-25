namespace backend.Dtos;

/// <summary>
/// 登录成功返回，对应 Apifox POST /auth/login 的 data
/// </summary>
public class LoginResponseDto
{
    public string token { get; set; } = string.Empty;

    public int userId { get; set; }

    public string username { get; set; } = string.Empty;

    public string? realName { get; set; }

    public string? roleName { get; set; }
}
