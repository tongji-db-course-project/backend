using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Route("returns"), Route("return-orders"), Authorize]
public class ReturnsController : ControllerBase
{
    private readonly IReturnService _service;
    public ReturnsController(IReturnService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int size = 10, string? keyword = null, string? status = null) =>
        Ok(ApiResponse<PageResult<ReturnOrderDto>>.Ok(await _service.ListAsync(page, size, keyword, status)));
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReturnRequest request) => await Execute(() => _service.CreateAsync(request));
    [HttpGet("{returnId:int}")]
    public async Task<IActionResult> Get(int returnId) => await Execute(() => _service.GetAsync(returnId));
    [HttpPost("{returnId:int}/confirm")]
    public async Task<IActionResult> Confirm(int returnId) => await Execute(() => _service.ConfirmAsync(returnId));
    // 兼容旧前端 business.ts 里遗留的 /returns/{id}/approve 调用，等效于 confirm
    [HttpPost("{returnId:int}/approve")]
    public async Task<IActionResult> Approve(int returnId) => await Execute(() => _service.ConfirmAsync(returnId));
    [HttpPost("{returnId:int}/reject")]
    public async Task<IActionResult> Reject(int returnId, [FromBody] RejectReturnRequest? request)
    {
        // 1) 优先信任前端显式传入的 approverId（>0 时）
        var operatorId = request?.approverId ?? 0;
        // 2) 前端未传 / 传 0 时，从当前 JWT 的 nameidentifier claim 里取登录用户 ID
        //    （项目 JwtSecurityToken 默认写的是 ClaimTypes.NameIdentifier，即 http://.../nameidentifier，不是 "UserId"）
        if (operatorId <= 0)
        {
            var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("UserId")?.Value
                       ?? User.FindFirst("sub")?.Value;
            if (int.TryParse(idClaim, out var uid) && uid > 0) operatorId = uid;
        }
        if (operatorId <= 0) return BadRequest(ApiResponse<object>.Fail(400, "无法识别审批人，请重新登录后再试"));
        return await Execute(() => _service.RejectAsync(returnId, operatorId, request?.remark));
    }
    [HttpGet("{returnId:int}/timeline")]
    public async Task<IActionResult> Timeline(int returnId) => await Execute(() => _service.GetTimelineAsync(returnId));

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(ApiResponse<T>.Ok(await action())); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(400, ex.Message)); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(404, ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.Fail(409, ex.Message)); }
    }
}

public class RejectReturnRequest
{
    /// <summary>审批人 ID，>0 时优先使用；0/null 时后端自动从当前 JWT 登录用户读取</summary>
    public int approverId { get; set; }
    public string? remark { get; set; }
}
