namespace backend.Dtos;

/// <summary>
/// 库存信息。字段与前端 InventoryItem 类型一致，商品/仓库信息已展平。
/// </summary>
public class InventoryDto
{
    public int inventoryId { get; set; }

    public int productId { get; set; }

    /// <summary>商品名称（关联商品表展平）</summary>
    public string? productName { get; set; }

    /// <summary>商品条码</summary>
    public string? barcode { get; set; }

    /// <summary>规格</summary>
    public string? specification { get; set; }

    /// <summary>单位</summary>
    public string? unit { get; set; }

    /// <summary>库存预警值（前端据此判断 正常/预警）</summary>
    public int? stockWarning { get; set; }

    public int warehouseId { get; set; }

    /// <summary>仓库名称（关联仓库表展平）</summary>
    public string? warehouseName { get; set; }

    public int currentStock { get; set; }

    public DateTime lastUpdateTime { get; set; }
}
