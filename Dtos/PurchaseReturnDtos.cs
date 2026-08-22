using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class PurchaseReturnDto
{
    public int returnId { get; set; }
    public string returnNo { get; set; } = string.Empty;
    public int purchaseId { get; set; }
    public string purchaseCode { get; set; } = string.Empty;
    public int supplierId { get; set; }
    public string supplierName { get; set; } = string.Empty;
    public int operatorId { get; set; }
    public string operatorName { get; set; } = string.Empty;
    public DateTime? returnDate { get; set; }
    public decimal totalAmount { get; set; }
    public string status { get; set; } = string.Empty;
    public DateTime? createTime { get; set; }
    public DateTime? updateTime { get; set; }
    public string? remark { get; set; }
    public List<PurchaseReturnDetailDto>? details { get; set; }
}

public class PurchaseReturnDetailDto
{
    public int productId { get; set; }
    public string productName { get; set; } = string.Empty;
    public int quantity { get; set; }
    public decimal returnPrice { get; set; }
    public decimal subtotal { get; set; }
}

public class SavePurchaseReturnRequest
{
    [Range(1, int.MaxValue)]
    public int purchaseId { get; set; }

    [Range(1, int.MaxValue)]
    public int operatorId { get; set; }

    public DateTime? returnDate { get; set; }

    [Required, MinLength(1)]
    public List<SavePurchaseReturnDetailRequest> details { get; set; } = new();

    [StringLength(200)]
    public string? remark { get; set; }
}

public class SavePurchaseReturnDetailRequest
{
    [Range(1, int.MaxValue)]
    public int productId { get; set; }

    [Range(1, int.MaxValue)]
    public int quantity { get; set; }
}

public class CompletePurchaseReturnRequest
{
    [Range(1, int.MaxValue)]
    public int operatorId { get; set; }

    [Range(1, int.MaxValue)]
    public int warehouseId { get; set; } = 1;

    [StringLength(200)]
    public string? remark { get; set; }
}
