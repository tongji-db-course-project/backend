using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class ReturnOrderDto
{
    public int returnId { get; set; }
    public string returnNo { get; set; } = string.Empty;
    public int saleId { get; set; }
    public string saleNo { get; set; } = string.Empty;
    public int? memberId { get; set; }
    public string? memberName { get; set; }
    public int operatorId { get; set; }
    public string? operatorName { get; set; }
    public DateTime returnDate { get; set; }
    public decimal refundAmount { get; set; }
    public string status { get; set; } = string.Empty;
    public DateTime? createTime { get; set; }
    public DateTime? updateTime { get; set; }
    public string? remark { get; set; }
    public List<ReturnOrderDetailDto>? items { get; set; }
}

public class ReturnOrderDetailDto
{
    public int productId { get; set; }
    public string productName { get; set; } = string.Empty;
    public string? barcode { get; set; }
    public int quantity { get; set; }
    public decimal refundPrice { get; set; }
    public decimal subtotal { get; set; }
}

public class CreateReturnRequest
{
    [Range(1, int.MaxValue)] public int saleId { get; set; }
    public int? memberId { get; set; }
    [Range(1, int.MaxValue)] public int operatorId { get; set; }
    [MaxLength(200)] public string? remark { get; set; }
    [Required, MinLength(1)] public List<CreateReturnDetailRequest> details { get; set; } = new();
}

public class CreateReturnDetailRequest
{
    [Range(1, int.MaxValue)] public int productId { get; set; }
    [Range(1, int.MaxValue)] public int quantity { get; set; }
    public decimal? refundPrice { get; set; }
}
