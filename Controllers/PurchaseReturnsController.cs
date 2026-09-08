using System.Security.Claims;
using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Route("purchase-returns"), Authorize(Roles = "1,2")]
public class PurchaseReturnsController : ControllerBase
{
    private readonly IPurchaseReturnService _service;
    public PurchaseReturnsController(IPurchaseReturnService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int size = 10, string? keyword = null, string? status = null,
        int? supplierId = null, int? purchaseId = null, DateTime? startDate = null, DateTime? endDate = null) =>
        await Execute(() => _service.ListAsync(page, size, keyword, status, supplierId, purchaseId, startDate, endDate));

    [HttpPost]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> Create([FromBody] SavePurchaseReturnRequest request)
    {
        if (!TryGetCurrentUserId(out var operatorId))
            return Unauthorized(ApiResponse<object>.Fail(401, "登录身份无效"));
        return await Execute(() => _service.CreateAsync(request, operatorId));
    }

    [HttpGet("{returnId:int}")]
    public async Task<IActionResult> Get(int returnId) => await Execute(() => _service.GetAsync(returnId));

    [HttpPut("{returnId:int}")]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> Update(int returnId, [FromBody] SavePurchaseReturnRequest request)
    {
        if (!TryGetCurrentUserId(out var operatorId))
            return Unauthorized(ApiResponse<object>.Fail(401, "登录身份无效"));
        return await Execute(() => _service.UpdateAsync(returnId, request, operatorId));
    }

    [HttpPost("{returnId:int}/approve")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Approve(int returnId, [FromBody] PurchaseReturnApprovalRequest request)
    {
        if (!TryGetCurrentUserId(out var approverId))
            return Unauthorized(ApiResponse<object>.Fail(401, "登录身份无效"));
        return await Execute(() => _service.ApproveAsync(returnId, request, approverId));
    }

    [HttpPost("{returnId:int}/complete")]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> Complete(int returnId, [FromBody] CompletePurchaseReturnRequest request)
    {
        if (!TryGetCurrentUserId(out var operatorId))
            return Unauthorized(ApiResponse<object>.Fail(401, "登录身份无效"));
        return await Execute(() => _service.CompleteAsync(returnId, request, operatorId));
    }

    [HttpDelete("{returnId:int}")]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> Cancel(int returnId)
    {
        try
        {
            if (!TryGetCurrentUserId(out var operatorId))
                return Unauthorized(ApiResponse<object>.Fail(401, "登录身份无效"));
            await _service.CancelAsync(returnId, operatorId);
            return Ok(ApiResponse<object?>.Ok(null, "采购退货单已作废"));
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            return Error(ex);
        }
    }

    [HttpGet("{returnId:int}/timeline")]
    public async Task<IActionResult> Timeline(int returnId) => await Execute(() => _service.GetTimelineAsync(returnId));

    [HttpGet("/suppliers/{supplierId:int}/purchase-returns")]
    public async Task<IActionResult> SupplierReturns(int supplierId, int page = 1, int size = 10) =>
        await Execute(() => _service.ListAsync(page, size, null, null, supplierId, null, null, null));

    [HttpGet("/purchases/{purchaseId:int}/returns")]
    public async Task<IActionResult> PurchaseReturns(int purchaseId, int page = 1, int size = 10) =>
        await Execute(() => _service.ListAsync(page, size, null, null, null, purchaseId, null, null));

    private bool TryGetCurrentUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;

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
