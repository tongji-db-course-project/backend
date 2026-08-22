using System.ComponentModel.DataAnnotations;

namespace backend.Dtos;

/// <summary>
/// 采购订单（主表 + 明细），字段与 Apifox PurchaseOrder 一致
/// </summary>
public class PurchaseOrderDto
{
    /// <summary>采购订单编号</summary>
    public int orderId { get; set; }

    /// <summary>采购单据编码</summary>
    public string orderCode { get; set; } = string.Empty;

    /// <summary>供应商编号</summary>
    public int? supplierId { get; set; }

    /// <summary>采购日期</summary>
    public DateTime? purchaseDate { get; set; }

    /// <summary>采购总金额</summary>
    public decimal? totalAmount { get; set; }

    /// <summary>申请人编号</summary>
    public int? applicantId { get; set; }

    /// <summary>审批人编号</summary>
    public int? approverId { get; set; }

    /// <summary>采购单状态：待审批/已驳回/已审批/已入库/已作废</summary>
    public string? status { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? createTime { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? updateTime { get; set; }

    /// <summary>采购明细行（列表接口为 null，详情接口返回）</summary>
    public List<PurchaseOrderDetailDto>? details { get; set; }
}

/// <summary>
/// 采购订单明细行
/// </summary>
public class PurchaseOrderDetailDto
{
    /// <summary>商品编号</summary>
    public int productId { get; set; }

    /// <summary>商品名称（关联查询展平）</summary>
    public string? productName { get; set; }

    /// <summary>采购数量</summary>
    public int? purchaseQuantity { get; set; }

    /// <summary>采购单价</summary>
    public decimal? purchasePrice { get; set; }

    /// <summary>行小计</summary>
    public decimal? lineTotal { get; set; }
}

/// <summary>
/// 创建/修改采购订单请求，字段与 Apifox PurchaseCreateDto 一致
/// </summary>
public class CreatePurchaseOrderRequest
{
    [Required(ErrorMessage = "供应商编号不能为空")]
    public int supplierId { get; set; }

    public DateTime? purchaseDate { get; set; }

    [StringLength(200)]
    public string? remark { get; set; }

    [Required(ErrorMessage = "申请人编号不能为空")]
    public int applicantId { get; set; }

    [Required(ErrorMessage = "采购明细不能为空")]
    public List<CreatePurchaseOrderDetailRequest> details { get; set; } = new();
}

/// <summary>
/// 创建采购订单明细行
/// </summary>
public class CreatePurchaseOrderDetailRequest
{
    [Required(ErrorMessage = "商品编号不能为空")]
    public int productId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "采购数量必须大于 0")]
    public int purchaseQuantity { get; set; }

    [Range(0, 99999999, ErrorMessage = "采购单价不合法")]
    public decimal purchasePrice { get; set; }
}

/// <summary>
/// 审批/驳回请求，字段与 Apifox ApprovalDto 一致
/// </summary>
public class ApprovalRequest
{
    [Required(ErrorMessage = "审批人编号不能为空")]
    public int approverId { get; set; }

    /// <summary>审批意见</summary>
    public string? remark { get; set; }
}

/// <summary>
/// 状态变更结果，字段与 Apifox OrderStatusResult 一致
/// </summary>
public class OrderStatusResultDto
{
    /// <summary>订单编号</summary>
    public int orderId { get; set; }

    /// <summary>单据编码</summary>
    public string orderCode { get; set; } = string.Empty;

    /// <summary>新状态</summary>
    public string status { get; set; } = string.Empty;

    /// <summary>操作人编号</summary>
    public int? operatorId { get; set; }

    /// <summary>状态变更时间</summary>
    public DateTime changeTime { get; set; }
}

/// <summary>
/// 采购入库请求，字段与 Apifox PurchaseStockInDto 一致
/// </summary>
public class PurchaseStockInRequest
{
    [Required(ErrorMessage = "入库操作人编号不能为空")]
    public int operatorId { get; set; }

    [Required(ErrorMessage = "入库仓库编号不能为空")]
    public int warehouseId { get; set; }

    [Required(ErrorMessage = "入库日期不能为空")]
    public DateTime stockInDate { get; set; }

    [Required(ErrorMessage = "入库明细不能为空")]
    public List<StockInDetailRequest> details { get; set; } = new();

    /// <summary>备注</summary>
    public string? remark { get; set; }
}

/// <summary>
/// 入库明细行
/// </summary>
public class StockInDetailRequest
{
    [Required(ErrorMessage = "商品编号不能为空")]
    public int productId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "实际入库数量必须大于 0")]
    public int stockInQuantity { get; set; }
}

/// <summary>
/// 入库结果，字段与 Apifox PurchaseStockInResult 一致
/// </summary>
public class PurchaseStockInResultDto
{
    /// <summary>采购订单编号</summary>
    public int orderId { get; set; }

    /// <summary>采购单据编码</summary>
    public string orderCode { get; set; } = string.Empty;

    /// <summary>采购单状态</summary>
    public string status { get; set; } = string.Empty;

    /// <summary>入库时间</summary>
    public DateTime stockInTime { get; set; }

    /// <summary>采购总金额</summary>
    public decimal totalAmount { get; set; }
}

public class OrderStatusLogDto
{
    public int logId { get; set; }
    public string orderType { get; set; } = string.Empty;
    public int orderId { get; set; }
    public string? oldStatus { get; set; }
    public string newStatus { get; set; } = string.Empty;
    public int? operatorId { get; set; }
    public DateTime? changeTime { get; set; }
    public string? remark { get; set; }
}
