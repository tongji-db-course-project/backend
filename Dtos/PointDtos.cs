using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class PointRecordDto
{
    public int pointRecordId { get; set; }
    public int memberId { get; set; }
    public string memberName { get; set; } = string.Empty;
    public int? saleId { get; set; }
    public string? saleNo { get; set; }
    public string changeType { get; set; } = string.Empty;
    public int changePoints { get; set; }
    public int remainPoints { get; set; }
    public DateTime? recordTime { get; set; }
    public string? remark { get; set; }
}

public class MemberPointsDto
{
    public int memberId { get; set; }
    public string memberName { get; set; } = string.Empty;
    public int points { get; set; }
    public PageResult<PointRecordDto> records { get; set; } = new();
}

public class AdjustPointsRequest
{
    [Range(-1000000, 1000000)]
    public int changePoints { get; set; }

    [Required, MaxLength(200)]
    public string remark { get; set; } = string.Empty;
}
