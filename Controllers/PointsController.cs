using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Authorize, ApiController]
public class PointsController : ControllerBase
{
    private readonly IPointService _service;
    public PointsController(IPointService service) => _service = service;

    [HttpGet("/points")]
    public async Task<IActionResult> List(int page = 1, int size = 10, int? memberId = null, string? changeType = null)
    {
        try { return Ok(ApiResponse<PageResult<PointRecordDto>>.Ok(await _service.ListAsync(page, size, memberId, changeType))); }
        catch (ArgumentException ex) { return Error(ex); }
    }

    [HttpGet("/members/{memberId:int}/points")]
    public async Task<IActionResult> GetMemberPoints(int memberId, int page = 1, int size = 10)
    {
        try { return Ok(ApiResponse<MemberPointsDto>.Ok(await _service.GetMemberPointsAsync(memberId, page, size))); }
        catch (KeyNotFoundException ex) { return Error(ex); }
    }

    [HttpPost("/members/{memberId:int}/points")]
    public async Task<IActionResult> Adjust(int memberId, [FromBody] AdjustPointsRequest request)
    {
        try { return Ok(ApiResponse<MemberPointsDto>.Ok(await _service.AdjustAsync(memberId, request), "积分调整成功")); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    private ObjectResult Error(Exception ex)
    {
        var status = ex is ArgumentException ? 400 : ex is KeyNotFoundException ? 404 : 409;
        return StatusCode(status, ApiResponse<object>.Fail(status, ex.Message));
    }
}
