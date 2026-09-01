namespace backend.Dtos;

/// <summary>
/// 商品请求参数（新增/修改共用）
/// </summary>
public class ProductDto
{
    public string productName { get; set; } = null!;

    public string? barcode { get; set; }

    public string? specification { get; set; }

    public decimal? purchasePrice { get; set; }

    public decimal? salePrice { get; set; }

    public string? isPromotion { get; set; }

    public decimal? promotionPrice { get; set; }

    public int? stockWarning { get; set; }

    public string? unit { get; set; }

    public string? status { get; set; }

    public int categoryId { get; set; }

    public int supplierId { get; set; }
}
