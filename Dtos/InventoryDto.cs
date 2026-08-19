namespace backend.Dtos;

/// <summary>
/// 库存信息。
/// </summary>
public class InventoryDto
{
    public int inventoryId { get; set; }

    public int productId { get; set; }

    public int warehouseId { get; set; }

    public int currentStock { get; set; }

    public DateTime lastUpdateTime { get; set; }
}
