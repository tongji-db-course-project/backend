using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class SettlementDto
{
    public int settlementId { get; set; }
    public int supplierId { get; set; }
    public string supplierName { get; set; } = string.Empty;
    public int purchaseId { get; set; }
    public string purchaseCode { get; set; } = string.Empty;
    public DateTime? settlementDate { get; set; }
    public DateTime? dueDate { get; set; }
    public decimal settlementAmount { get; set; }
    public decimal paidAmount { get; set; }
    public decimal unpaidAmount { get; set; }
    public string status { get; set; } = string.Empty;
    public bool overdue { get; set; }
    public string? remark { get; set; }
}

public class CreateSettlementRequest
{
    [Range(1, int.MaxValue)] public int supplierId { get; set; }
    [Range(1, int.MaxValue)] public int purchaseId { get; set; }
    public DateTime? settlementDate { get; set; }
    [Range(0.01, 999999999)] public decimal settlementAmount { get; set; }
    [Range(0, 999999999)] public decimal paidAmount { get; set; }
    [MaxLength(200)] public string? remark { get; set; }
}

public class PaySettlementRequest
{
    [Range(0.01, 999999999)] public decimal paidAmount { get; set; }
    [MaxLength(200)] public string? remark { get; set; }
}
