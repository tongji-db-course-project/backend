using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

/// <summary>
/// 手动调整库存请求。
/// </summary>
public class InventoryAdjustDto
{
    [Range(1, int.MaxValue)]
    public int productId { get; set; }

    public int changeQty { get; set; }

    /// <summary>盘点时提交的实际库存；普通手动出入库不使用。</summary>
    [Range(0, int.MaxValue)]
    public int? actualStock { get; set; }

    [Required]
    [StringLength(20)]
    public string recordType { get; set; } = string.Empty;

    [StringLength(200)]
    public string? remark { get; set; }

    [StringLength(50)]
    public string? sourceNo { get; set; }
}

public class InventoryAdjustByProductRequest
{
    public int changeQty { get; set; }
    [Range(0, int.MaxValue)] public int? actualStock { get; set; }
    [Required, StringLength(20)] public string recordType { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int operatorId { get; set; }
    [StringLength(200)] public string? remark { get; set; }
    [StringLength(50)] public string? sourceNo { get; set; }
}
