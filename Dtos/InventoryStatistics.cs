namespace backend.Dtos;

/// <summary>
/// 库存统计返回结构
/// </summary>
public class InventoryStatistics
{
    /// <summary>
    /// 商品总数
    /// </summary>
    public long ProductCount { get; set; }

    /// <summary>
    /// 库存总量
    /// </summary>
    public long TotalStock { get; set; }

    /// <summary>
    /// 库存预警商品数量
    /// </summary>
    public long WarningProductCount { get; set; }

    /// <summary>
    /// 仓库数量
    /// </summary>
    public long WarehouseCount { get; set; }
}
