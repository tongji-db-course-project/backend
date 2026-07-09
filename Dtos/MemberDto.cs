namespace backend.Dtos;

/// <summary>
/// 会员请求参数（新增/修改共用）
/// </summary>
public class MemberDto
{
    public string memberName { get; set; } = string.Empty;

    public string phone { get; set; } = string.Empty;

    public string? gender { get; set; }

    public string? levelName { get; set; }
}