using System.Security.Claims;
using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Authorize, ApiController, Route("sales")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _service;
    public SalesController(ISaleService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int size = 10, string? keyword = null, string? status = null,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        try { return Ok(ApiResponse<PageResult<SaleListItemDto>>.Ok(await _service.ListAsync(page, size, keyword, status, startDate, endDate))); }
        catch (ArgumentException ex) { return Error(ex); }
    }

    [HttpGet("{saleId:int}")]
    public async Task<IActionResult> Get(int saleId)
    {
        try { return Ok(ApiResponse<SaleDetailDto>.Ok(await _service.GetAsync(saleId))); }
        catch (KeyNotFoundException ex) { return Error(ex); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CheckoutSaleRequest request)
    {
        try
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claim, out var userId)) return Unauthorized(ApiResponse<object>.Fail(401, "登录身份无效"));
            return Ok(ApiResponse<SaleDetailDto>.Ok(await _service.CreateAsync(ToCommand(request), userId), "销售结算成功"));
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutSaleRequest request)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId)) return Unauthorized(ApiResponse<object>.Fail(401, "登录身份无效"));
        var command = ToCommand(request);
        try { return Ok(ApiResponse<SaleDetailDto>.Ok(await _service.CreateAsync(command, userId), "销售结算成功")); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpDelete("{saleId:int}")]
    public async Task<IActionResult> Delete(int saleId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(claim, out var userId)) return Unauthorized(ApiResponse<object>.Fail(401, "登录身份无效"));
        try { await _service.CancelAsync(saleId, userId); return Ok(ApiResponse<object?>.Ok(null, "销售单已作废")); }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpGet("{saleId:int}/timeline")]
    public async Task<IActionResult> Timeline(int saleId)
    {
        try { return Ok(ApiResponse<IReadOnlyList<OrderStatusLogDto>>.Ok(await _service.GetTimelineAsync(saleId))); }
        catch (KeyNotFoundException ex) { return Error(ex); }
    }

    private ObjectResult Error(Exception ex)
    {
        var status = ex is ArgumentException ? 400 : ex is KeyNotFoundException ? 404 : 409;
        return StatusCode(status, ApiResponse<object>.Fail(status, ex.Message));
    }

    private static CreateSaleRequest ToCommand(CheckoutSaleRequest request)
    {
        var items = request.items ?? request.details?.Select(x => new CreateSaleItemRequest
        {
            productId = x.productId,
            quantity = x.saleQuantity
        }).ToList() ?? new List<CreateSaleItemRequest>();
        return new CreateSaleRequest
        {
            memberId = request.memberId,
            couponId = request.couponId,
            warehouseId = request.warehouseId,
            payType = request.payType,
            redeemPoints = request.redeemPoints,
            items = items
        };
    }
}
