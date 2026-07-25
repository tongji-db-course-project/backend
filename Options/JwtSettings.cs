namespace backend.Options;

/// <summary>
/// JWT 配置，对应 appsettings.json 的 Jwt 节点
/// </summary>
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpireHours { get; set; } = 8;
}
