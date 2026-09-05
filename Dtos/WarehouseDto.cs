namespace backend.Dtos;

/// <summary>
/// 仓库信息，字段与前端 Warehouse 类型一致（camelCase）
/// </summary>
public class WarehouseDto
{
    public int warehouseId { get; set; }

    public string warehouseName { get; set; } = string.Empty;

    public string? address { get; set; }

    public string? status { get; set; }

    public DateTime? createTime { get; set; }
}
