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
    [HttpPost("{returnId:int}/reject")]
    public async Task<IActionResult> Reject(int returnId, [FromBody] RejectReturnRequest? request) =>
        await Execute(() => _service.RejectAsync(returnId, request));
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
