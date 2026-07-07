namespace backend.Dtos;

/// <summary>
/// 商品列表项：商品基础信息 + 类别/供应商名称 + 当前库存
/// </summary>
public class ProductListItemDto
{
    public int productId { get; set; }

    public string productName { get; set; } = string.Empty;

    public string? barcode { get; set; }

    public string? specification { get; set; }

    public decimal? purchasePrice { get; set; }

    public decimal? salePrice { get; set; }

    public int? stockWarning { get; set; }

    public string? unit { get; set; }

    public string? status { get; set; }

    public int categoryId { get; set; }

    public string? categoryName { get; set; }

    public int supplierId { get; set; }

    public string? supplierName { get; set; }

    /// <summary>当前库存量（无库存记录时为 0）</summary>
    public int currentStock { get; set; }
}
