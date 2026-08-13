using System.Text.Json.Serialization;

namespace backend.Dtos;

/// <summary>
/// 商品列表项：商品基础信息、分类与供应商名称、所有仓库库存总量。
/// </summary>
public class ProductListItemDto
{
    [JsonPropertyName("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("productName")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    [JsonPropertyName("specification")]
    public string? Specification { get; set; }

    [JsonPropertyName("purchasePrice")]
    public decimal? PurchasePrice { get; set; }

    [JsonPropertyName("salePrice")]
    public decimal? SalePrice { get; set; }

    [JsonPropertyName("stockWarning")]
    public int? StockWarning { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("categoryId")]
    public int CategoryId { get; set; }

    [JsonPropertyName("categoryName")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("supplierId")]
    public int SupplierId { get; set; }

    [JsonPropertyName("supplierName")]
    public string? SupplierName { get; set; }

    /// <summary>所有仓库的当前库存合计，无库存记录时为 0。</summary>
    [JsonPropertyName("currentStock")]
    public int CurrentStock { get; set; }
}
