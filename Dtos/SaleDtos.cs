using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class CreateSaleRequest
{
    public int? memberId { get; set; }

    [Range(1, int.MaxValue)]
    public int warehouseId { get; set; }

    [Required, MaxLength(20)]
    public string payType { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int redeemPoints { get; set; }

    [Required, MinLength(1)]
    public List<CreateSaleItemRequest> items { get; set; } = new();
}

public class CreateSaleItemRequest
{
    [Range(1, int.MaxValue)]
    public int productId { get; set; }

    [Range(1, int.MaxValue)]
    public int quantity { get; set; }
}

public class SaleListItemDto
{
    public int saleId { get; set; }
    public string saleNo { get; set; } = string.Empty;
    public int? memberId { get; set; }
    public string? memberName { get; set; }
    public int userId { get; set; }
    public string? cashierName { get; set; }
    public DateTime? saleDate { get; set; }
    public decimal totalAmount { get; set; }
    public decimal discountAmount { get; set; }
    public decimal paidAmount { get; set; }
    public string? payType { get; set; }
    public string? status { get; set; }
}

public class SaleDetailDto : SaleListItemDto
{
    public int redeemedPoints { get; set; }
    public int earnedPoints { get; set; }
    public List<SaleDetailItemDto> items { get; set; } = new();
}

public class SaleDetailItemDto
{
    public int productId { get; set; }
    public string productName { get; set; } = string.Empty;
    public int quantity { get; set; }
    public decimal salePrice { get; set; }
    public decimal subtotal { get; set; }
}
