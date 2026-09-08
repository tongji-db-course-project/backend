namespace backend.Dtos;

public class ProductProfitRankDto
{
    public int productId { get; set; }
    public string productName { get; set; } = string.Empty;
    public int netSaleQuantity { get; set; }
    public decimal netSaleAmount { get; set; }
    public decimal purchaseCost { get; set; }
    public decimal grossProfit { get; set; }
    public decimal grossProfitRate { get; set; }
    public decimal profitContributionRate { get; set; }
}

public class InventoryTurnoverDto
{
    public int productId { get; set; }
    public string productName { get; set; } = string.Empty;
    public int soldQuantity { get; set; }
    public decimal beginningStock { get; set; }
    public decimal endingStock { get; set; }
    public decimal averageStock { get; set; }
    public decimal turnoverTimes { get; set; }
    public bool stagnant { get; set; }
    public decimal? daysOfInventory { get; set; }
    //周转状态：normal，slow，aged
    public string status { get; set; } = "normal";
}

public class DailySettlementDto
{
    public int settlementId { get; set; }
    public DateTime settlementDate { get; set; }
    public decimal totalSales { get; set; }
    public decimal refundAmount { get; set; }
    public decimal netSales { get; set; }
    public decimal cashAmount { get; set; }
    public decimal wechatAmount { get; set; }
    public decimal alipayAmount { get; set; }
    public decimal promotionDiscount { get; set; }
    public decimal memberDiscount { get; set; }
    public decimal couponDeduct { get; set; }
    public decimal pointDeduct { get; set; }
    public int pointConsumed { get; set; }
    public int orderCount { get; set; }
    public string status { get; set; } = string.Empty;
    public DateTime? createTime { get; set; }
}
