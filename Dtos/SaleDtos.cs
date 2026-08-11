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

/// <summary>
/// 销售订单列表项（含会员/收银员冗余字段），与会员消费记录接口共用
/// </summary>
public class SaleListItemDto : SaleOrderListItem
{
    public int? memberId { get; set; }
    public string? memberName { get; set; }
    public int userId { get; set; }
    public string? cashierName { get; set; }
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
