namespace backend.Dtos;

/// <summary>
/// 库存信息。
/// </summary>
public class InventoryDto
{
    public int inventoryId { get; set; }

    public int productId { get; set; }

    public string productName { get; set; } = string.Empty;

    public string? barcode { get; set; }

    public string? specification { get; set; }

    public string? unit { get; set; }

    public int? stockWarning { get; set; }

    public int warehouseId { get; set; }

    public int currentStock { get; set; }

    public DateTime? lastUpdateTime { get; set; }
}
