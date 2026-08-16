using System.Security.Claims;
using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet(Name = "listInventory")]
    public async Task<IActionResult> ListInventory(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null,
        [FromQuery] int? productId = null)
    {
        return await ExecuteAsync(async () =>
            ApiResponse<PageResult<InventoryDto>>.Ok(
                await _inventoryService.ListInventoryAsync(
                    page, size, keyword, status, productId)));
    }

    [HttpGet("products/{productId:int}", Name = "getInventoryByProduct")]
    public async Task<IActionResult> GetInventoryByProduct([FromRoute] int productId)
    {
        return await ExecuteAsync(async () =>
            ApiResponse<InventoryDto>.Ok(
                await _inventoryService.GetInventoryByProductAsync(productId)));
    }

    [HttpGet("records", Name = "listInventoryRecords")]
    public async Task<IActionResult> ListInventoryRecords(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] int? productId = null,
        [FromQuery] string? recordType = null)
    {
        return await ExecuteAsync(async () =>
            ApiResponse<PageResult<InventoryRecordDto>>.Ok(
                await _inventoryService.ListRecordsAsync(
                    page, size, keyword, productId, recordType)));
    }

    [HttpGet("{inventoryId:int}", Name = "getInventory")]
    public async Task<IActionResult> GetInventory([FromRoute] int inventoryId)
    {
        return await ExecuteAsync(async () =>
            ApiResponse<InventoryDto>.Ok(
                await _inventoryService.GetInventoryAsync(inventoryId)));
    }

    [HttpGet("warning", Name = "listInventoryWarning")]
    public async Task<IActionResult> ListInventoryWarning(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null)
    {
        return await ExecuteAsync(async () =>
            ApiResponse<PageResult<InventoryDto>>.Ok(
                await _inventoryService.ListWarningAsync(
                    page, size, keyword, status)));
    }

    [Authorize(Roles = "1")]
    [HttpPut("adjust", Name = "adjustInventory")]
    public async Task<IActionResult> AdjustInventory([FromBody] InventoryAdjustDto request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var operatorId))
            return Unauthorized(ApiResponse<object>.Fail(401, "登录状态无效"));

        return await ExecuteAsync(async () =>
            ApiResponse<InventoryDto>.Ok(
                await _inventoryService.AdjustInventoryAsync(request, operatorId)));
    }

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<ApiResponse<T>>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (BusinessException ex)
        {
            return StatusCode(ex.Code, ApiResponse<object>.Fail(ex.Code, ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(404, ex.Message));
        }
    }
}
