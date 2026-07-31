namespace backend.Dtos;

/// <summary>
/// 会员响应结构
/// </summary>
public class Member
{
    public int memberId { get; set; }

    public string memberName { get; set; } = string.Empty;

    public string phone { get; set; } = string.Empty;

    public string? gender { get; set; }

    public string? levelName { get; set; }

    public int? points { get; set; }

    public decimal? totalAmount { get; set; }

    public DateTime? registerTime { get; set; }

    public string? status { get; set; }
}