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

    [Required]
    [StringLength(20)]
    public string recordType { get; set; } = string.Empty;

    [StringLength(200)]
    public string? remark { get; set; }

    [StringLength(50)]
    public string? sourceNo { get; set; }
}
