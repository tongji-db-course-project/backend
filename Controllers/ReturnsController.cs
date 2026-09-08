using System.Security.Claims;
using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Route("return-orders"), Authorize(Roles = "1,2,3")]
public class ReturnsController : ControllerBase
{
    private readonly IReturnService _service;
    public ReturnsController(IReturnService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int size = 10, string? keyword = null, string? status = null) =>
        Ok(ApiResponse<PageResult<ReturnOrderDto>>.Ok(await _service.ListAsync(page, size, keyword, status)));

    [HttpPost]
    [Authorize(Roles = "3")]
    public async Task<IActionResult> Create([FromBody] CreateReturnRequest request)
    {
        if (!TryGetCurrentUserId(out var operatorId))
            return Unauthorized(ApiResponse<object>.Fail(401, "登录身份无效"));
        return await Execute(() => _service.CreateAsync(request, operatorId));
    }

    [HttpGet("{returnId:int}")]
    public async Task<IActionResult> Get(int returnId) => await Execute(() => _service.GetAsync(returnId));

    [HttpPost("{returnId:int}/approve")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Approve(int returnId)
    {
        if (!TryGetCurrentUserId(out var approverId))
            return Unauthorized(ApiResponse<object>.Fail(401, "登录身份无效"));
        return await Execute(() => _service.ApproveAsync(returnId, approverId));
    }

    [HttpPost("{returnId:int}/complete")]
    [Authorize(Roles = "2")]
    public async Task<IActionResult> Complete(int returnId)
    {
        if (!TryGetCurrentUserId(out var operatorId))
            return Unauthorized(ApiResponse<object>.Fail(401, "登录身份无效"));
        return await Execute(() => _service.CompleteAsync(returnId, operatorId));
    }

    [HttpPost("{returnId:int}/reject")]
    [Authorize(Roles = "1")]
    public async Task<IActionResult> Reject(int returnId, [FromBody] RejectReturnRequest request)
    {
        if (!TryGetCurrentUserId(out var approverId))
            return Unauthorized(ApiResponse<object>.Fail(401, "登录身份无效"));
        return await Execute(() => _service.RejectAsync(returnId, approverId, request?.remark));
    }

    [HttpGet("{returnId:int}/timeline")]
    public async Task<IActionResult> Timeline(int returnId) => await Execute(() => _service.GetTimelineAsync(returnId));

    private bool TryGetCurrentUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0;

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(ApiResponse<T>.Ok(await action())); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(400, ex.Message)); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(404, ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.Fail(409, ex.Message)); }
    }
}
