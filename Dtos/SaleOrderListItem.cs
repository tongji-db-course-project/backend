namespace backend.Dtos;

/// <summary>
/// 销售订单列表项
/// </summary>
public class SaleOrderListItem
{
    public int saleId { get; set; }

    public string saleNo { get; set; } = string.Empty;

    public DateTime? saleDate { get; set; }

    public decimal? totalAmount { get; set; }

    public decimal? discountAmount { get; set; }

    public decimal? paidAmount { get; set; }

    public string? payType { get; set; }

    public string? status { get; set; }
}