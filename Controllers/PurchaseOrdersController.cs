using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>
/// 采购订单管理
/// </summary>
[ApiController]
[Route("purchase-orders")]
[Authorize]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;

    public PurchaseOrdersController(IPurchaseOrderService purchaseOrderService)
    {
        _purchaseOrderService = purchaseOrderService;
    }

    /// <summary>
    /// 查询采购订单列表（分页 + 关键词 + 状态 + 供应商过滤）
    /// </summary>
    [HttpGet(Name = "listPurchaseOrders")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<PurchaseOrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOrders(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null,
        [FromQuery] int? supplierId = null)
    {
        var result = await _purchaseOrderService.ListOrdersAsync(page, size, keyword, status, supplierId);
        return Ok(ApiResponse<PageResult<PurchaseOrderDto>>.Ok(result));
    }

    /// <summary>
    /// 创建采购订单（初始为待审批状态）
    /// </summary>
    [HttpPost(Name = "createPurchaseOrder")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrder([FromBody] CreatePurchaseOrderRequest request)
    {
        try
        {
            var result = await _purchaseOrderService.CreateOrderAsync(request);
            return Ok(ApiResponse<PurchaseOrderDto>.Ok(result));
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException)
        {
            return Error(ex);
        }
    }

    /// <summary>
    /// 查询采购订单详情（含明细行）
    /// </summary>
    [HttpGet("{orderId:int}", Name = "getPurchaseOrder")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrder([FromRoute] int orderId)
    {
        try
        {
            var result = await _purchaseOrderService.GetOrderAsync(orderId);
            return Ok(ApiResponse<PurchaseOrderDto>.Ok(result));
        }
        catch (Exception ex) when (ex is KeyNotFoundException)
        {
            return Error(ex);
        }
    }

    /// <summary>
    /// 修改采购订单（仅待审批状态可修改）
    /// </summary>
    [HttpPut("{orderId:int}", Name = "updatePurchaseOrder")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOrder(
        [FromRoute] int orderId,
        [FromBody] CreatePurchaseOrderRequest request)
    {
        try
        {
            var result = await _purchaseOrderService.UpdateOrderAsync(orderId, request);
            return Ok(ApiResponse<PurchaseOrderDto>.Ok(result));
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            return Error(ex);
        }
    }

    /// <summary>
    /// 作废采购订单（置为已作废，不物理删除）
    /// </summary>
    [HttpDelete("{orderId:int}", Name = "cancelPurchaseOrder")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelOrder([FromRoute] int orderId)
    {
        try
        {
            await _purchaseOrderService.CancelOrderAsync(orderId);
            return Ok(ApiResponse<object?>.Ok(null));
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return Error(ex);
        }
    }

    /// <summary>
    /// 审批通过采购订单（待审批 → 已审批）
    /// </summary>
    [HttpPost("{orderId:int}/approve", Name = "approvePurchaseOrder")]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveOrder(
        [FromRoute] int orderId,
        [FromBody] ApprovalRequest request)
    {
        try
        {
            var result = await _purchaseOrderService.ApproveOrderAsync(orderId, request);
            return Ok(ApiResponse<OrderStatusResultDto>.Ok(result));
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            return Error(ex);
        }
    }

    /// <summary>
    /// 驳回采购订单（保持待审批，驳回理由记入日志，可修改后再次审批）
    /// </summary>
    [HttpPost("{orderId:int}/reject", Name = "rejectPurchaseOrder")]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectOrder(
        [FromRoute] int orderId,
        [FromBody] ApprovalRequest request)
    {
        try
        {
            var result = await _purchaseOrderService.RejectOrderAsync(orderId, request);
            return Ok(ApiResponse<OrderStatusResultDto>.Ok(result));
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            return Error(ex);
        }
    }

    /// <summary>
    /// 采购入库（加库存 + 记流水 + 生成结算，事务保证原子性）
    /// </summary>
    [HttpPost("{orderId:int}/stock-in", Name = "stockInPurchaseOrder")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseStockInResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> StockIn(
        [FromRoute] int orderId,
        [FromBody] PurchaseStockInRequest request)
    {
        try
        {
            var result = await _purchaseOrderService.StockInAsync(orderId, request);
            return Ok(ApiResponse<PurchaseStockInResultDto>.Ok(result));
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            return Error(ex);
        }
    }

    private ObjectResult Error(Exception ex)
    {
        var statusCode = ex switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(statusCode, new ApiResponse<object>
        {
            code = statusCode,
            message = ex.Message,
            data = null
        });
    }
}
