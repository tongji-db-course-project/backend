using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace backend.Dtos;

public class Product
{
    [JsonPropertyName("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("categoryId")]
    public int CategoryId { get; set; }

    [JsonPropertyName("supplierId")]
    public int SupplierId { get; set; }

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
}

public class ProductDto
{
    [Required(ErrorMessage = "商品分类不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "商品分类不能为空")]
    [JsonPropertyName("categoryId")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "供应商不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "供应商不能为空")]
    [JsonPropertyName("supplierId")]
    public int? SupplierId { get; set; }

    [Required(ErrorMessage = "商品名称不能为空")]
    [StringLength(100, ErrorMessage = "商品名称不能超过100个字符")]
    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }

    [Required(ErrorMessage = "商品条码不能为空")]
    [StringLength(50, ErrorMessage = "商品条码不能超过50个字符")]
    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    [StringLength(100, ErrorMessage = "商品规格不能超过100个字符")]
    [JsonPropertyName("specification")]
    public string? Specification { get; set; }

    [Required(ErrorMessage = "采购价格不能为空")]
    [Range(typeof(decimal), "0", "99999999.99", ErrorMessage = "采购价格超出允许范围")]
    [JsonPropertyName("purchasePrice")]
    public decimal? PurchasePrice { get; set; }

    [Required(ErrorMessage = "销售价格不能为空")]
    [Range(typeof(decimal), "0", "99999999.99", ErrorMessage = "销售价格超出允许范围")]
    [JsonPropertyName("salePrice")]
    public decimal? SalePrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "库存预警值不能小于0")]
    [JsonPropertyName("stockWarning")]
    public int? StockWarning { get; set; }

    [StringLength(20, ErrorMessage = "商品单位不能超过20个字符")]
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [StringLength(20, ErrorMessage = "商品状态不能超过20个字符")]
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
