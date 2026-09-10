using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

public class CreateStockCheckRequest
{
    [Range(1, int.MaxValue)]
    public int productId { get; set; }

    [StringLength(200)] public string? remark { get; set; }
}

public class ConfirmStockCheckRequest
{
    [Required, MinLength(1)]
    public List<StockCheckActualItem> items { get; set; } = [];

    [StringLength(200)] public string? remark { get; set; }
}

public class StockCheckActualItem
{
    [Range(1, int.MaxValue)] public int productId { get; set; }
    [Required, Range(0, int.MaxValue)] public int? actualQty { get; set; }
}

public class StockCheckListItemDto
{
    public int checkId { get; set; }
    public string checkNo { get; set; } = string.Empty;
    public string productName { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public string operatorName { get; set; } = string.Empty;
    public DateTime? checkDate { get; set; }
    public DateTime? completeDate { get; set; }
    public string? remark { get; set; }
}

public class StockCheckDetailDto : StockCheckListItemDto
{
    public List<StockCheckDetailItemDto> items { get; set; } = [];
}

public class StockCheckDetailItemDto
{
    public int productId { get; set; }
    public string productName { get; set; } = string.Empty;
    public string? barcode { get; set; }
    public string? unit { get; set; }
    public int systemQty { get; set; }
    public int? actualQty { get; set; }
    public int? differenceQty { get; set; }
    public decimal? adjustPrice { get; set; }
    public decimal? adjustAmount { get; set; }
    public string resultType => differenceQty switch { > 0 => "盘盈", < 0 => "盘亏", 0 => "无差异", _ => "待盘点" };
}
