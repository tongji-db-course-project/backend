using System.Security.Claims;
using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Route("inventory/counts"), Authorize(Roles = "1")]
public class StockChecksController : ControllerBase
{
    private readonly IStockCheckService _service;
    public StockChecksController(IStockCheckService service) => _service = service;

    [HttpGet] public async Task<IActionResult> List(int page = 1, int size = 10, string? status = null) =>
        Ok(ApiResponse<PageResult<StockCheckListItemDto>>.Ok(await _service.ListAsync(page, size, status)));
    [HttpGet("{checkId:int}")] public async Task<IActionResult> Get(int checkId) =>
        Ok(ApiResponse<StockCheckDetailDto>.Ok(await _service.GetAsync(checkId)));
    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateStockCheckRequest request) =>
        Ok(ApiResponse<StockCheckDetailDto>.Ok(await _service.CreateAsync(request, UserId())));
    [HttpPost("{checkId:int}/confirm")] public async Task<IActionResult> Confirm(int checkId, [FromBody] ConfirmStockCheckRequest request) =>
        Ok(ApiResponse<StockCheckDetailDto>.Ok(await _service.ConfirmAsync(checkId, request, UserId())));
    [HttpPost("{checkId:int}/cancel")] public async Task<IActionResult> Cancel(int checkId) { await _service.CancelAsync(checkId, UserId()); return Ok(ApiResponse<object?>.Ok(null)); }

    private int UserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new BusinessException(401, "登录状态无效");
}
