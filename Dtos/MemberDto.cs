namespace backend.Dtos;

/// <summary>
/// 会员请求参数
/// </summary>
public class MemberDto
{
    public string? memberName { get; set; }

    public string? phone { get; set; }

    public string? gender { get; set; }

    public string? levelName { get; set; }

    public string? status { get; set; }
}