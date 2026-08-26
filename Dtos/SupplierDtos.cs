using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class SupplierDto
{
    public int supplierId { get; set; }
    public string supplierName { get; set; } = string.Empty;
    public string? contactPerson { get; set; }
    public string? phone { get; set; }
    public string? email { get; set; }
    public string? address { get; set; }
    public string? creditLevel { get; set; }
    public short? paymentCycle { get; set; }
    public int? minOrderQty { get; set; }
    public string? bankName { get; set; }
    public string? bankAccount { get; set; }
    public string? status { get; set; }
}

public class SaveSupplierRequest
{
    [Required, StringLength(100)] public string supplierName { get; set; } = string.Empty;
    [StringLength(50)] public string? contactPerson { get; set; }
    [StringLength(20)] public string? phone { get; set; }
    [EmailAddress, StringLength(100)] public string? email { get; set; }
    [StringLength(200)] public string? address { get; set; }
    [StringLength(20)] public string? creditLevel { get; set; }
    [Range(0, short.MaxValue)] public short? paymentCycle { get; set; }
    [Range(0, int.MaxValue)] public int? minOrderQty { get; set; }
    [StringLength(100)] public string? bankName { get; set; }
    [StringLength(50)] public string? bankAccount { get; set; }
    [RegularExpression("^(启用|禁用)$")] public string? status { get; set; }
}

public class SupplierPerformanceDto
{
    public int supplierId { get; set; }
    public string supplierName { get; set; } = string.Empty;
    public int stockedOrderCount { get; set; }
    public int returnedOrderCount { get; set; }
    public decimal returnRate { get; set; }
    public decimal onTimeRate { get; set; }
    public string creditLevel { get; set; } = string.Empty;
}
