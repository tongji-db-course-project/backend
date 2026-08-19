namespace backend.Dtos;

/// <summary>
/// 库存变动流水。
/// </summary>
public class InventoryRecordDto
{
    public int recordId { get; set; }

    public int productId { get; set; }

    public string recordType { get; set; } = string.Empty;

    public string? sourceNo { get; set; }

    public int changeQty { get; set; }

    public int remainQty { get; set; }

    public int operatorId { get; set; }

    public DateTime recordTime { get; set; }

    public string? remark { get; set; }
}
