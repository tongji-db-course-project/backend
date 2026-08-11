namespace backend.Dtos;

/// <summary>
/// 商品销量排行返回结构
/// </summary>
public class ProductRank
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// 商品名称
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 销售数量
    /// </summary>
    public int SaleQuantity { get; set; }

    /// <summary>
    /// 销售金额
    /// </summary>
    public decimal SaleAmount { get; set; }
}
