using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Route("purchases"), Authorize]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseOrderService _service;
    public PurchasesController(IPurchaseOrderService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int size = 10, string? keyword = null, string? status = null, int? supplierId = null) =>
        Ok(ApiResponse<PageResult<PurchaseOrderDto>>.Ok(await _service.ListOrdersAsync(page, size, keyword, status, supplierId)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request) =>
        await Execute(() => _service.CreateOrderAsync(request));

    [HttpGet("{purchaseId:int}")]
    public async Task<IActionResult> Get(int purchaseId) => await Execute(() => _service.GetOrderAsync(purchaseId));

    [HttpPut("{purchaseId:int}")]
    public async Task<IActionResult> Update(int purchaseId, [FromBody] CreatePurchaseOrderRequest request) =>
        await Execute(() => _service.UpdateOrderAsync(purchaseId, request));

    [HttpDelete("{purchaseId:int}")]
    public async Task<IActionResult> Delete(int purchaseId)
    {
        try { await _service.CancelOrderAsync(purchaseId); return Ok(ApiResponse<object?>.Ok(null, "删除成功")); }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpPost("{purchaseId:int}/submit")]
    public async Task<IActionResult> Submit(int purchaseId) => await Execute(() => _service.SubmitOrderAsync(purchaseId));

    [HttpPost("{purchaseId:int}/approve")]
    public async Task<IActionResult> Approve(int purchaseId, [FromBody] ApprovalRequest request) =>
        await Execute(() => _service.ApproveOrderAsync(purchaseId, request));

    [HttpPost("{purchaseId:int}/reject")]
    public async Task<IActionResult> Reject(int purchaseId, [FromBody] ApprovalRequest request) =>
        await Execute(() => _service.RejectOrderAsync(purchaseId, request));

    [HttpPost("{purchaseId:int}/stock-in")]
    public async Task<IActionResult> StockIn(int purchaseId, [FromBody] PurchaseStockInRequest request) =>
        await Execute(() => _service.StockInAsync(purchaseId, request));

    [HttpGet("{purchaseId:int}/timeline")]
    public async Task<IActionResult> Timeline(int purchaseId) => await Execute(() => _service.GetTimelineAsync(purchaseId));

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(ApiResponse<T>.Ok(await action())); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    private ObjectResult Error(Exception ex)
    {
        var status = ex is ArgumentException ? 400 : ex is KeyNotFoundException ? 404 : 409;
        return StatusCode(status, ApiResponse<object>.Fail(status, ex.Message));
    }
}
