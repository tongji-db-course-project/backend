using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Authorize, ApiController, Route("points")]
public class PointsController : ControllerBase
{
    private readonly IPointService _service;
    public PointsController(IPointService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int size = 10, int? memberId = null, string? changeType = null, string? keyword = null)
    {
        try { return Ok(ApiResponse<PageResult<PointRecordDto>>.Ok(await _service.ListAsync(page, size, memberId, changeType, keyword))); }
        catch (ArgumentException ex) { return Error(ex); }
    }

    [HttpGet("records")]
    public Task<IActionResult> ListRecords(int page = 1, int size = 10, int? memberId = null, string? keyword = null, string? status = null) =>
        List(page, size, memberId, status, keyword);

    [HttpGet("~/members/{memberId:int}/points")]
    public async Task<IActionResult> GetMemberPoints(int memberId, int page = 1, int size = 10)
    {
        try { return Ok(ApiResponse<MemberPointsDto>.Ok(await _service.GetMemberPointsAsync(memberId, page, size))); }
        catch (KeyNotFoundException ex) { return Error(ex); }
    }

    [HttpPost("~/members/{memberId:int}/points")]
    public async Task<IActionResult> Adjust(int memberId, [FromBody] AdjustPointsRequest request)
    {
        try { return Ok(ApiResponse<MemberPointsDto>.Ok(await _service.AdjustAsync(memberId, request), "积分调整成功")); }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return Error(ex); }
    }

    [HttpPost("~/members/{memberId:int}/points/add")]
    public Task<IActionResult> Add(int memberId, [FromBody] ManualPointsRequest request) =>
        Adjust(memberId, new AdjustPointsRequest
        {
            changePoints = request.changePoints,
            remark = string.IsNullOrWhiteSpace(request.remark) ? $"操作人 {request.operatorId} 手工增加" : request.remark
        });

    [HttpPost("~/members/{memberId:int}/points/deduct")]
    public Task<IActionResult> Deduct(int memberId, [FromBody] ManualPointsRequest request) =>
        Adjust(memberId, new AdjustPointsRequest
        {
            changePoints = -request.changePoints,
            remark = string.IsNullOrWhiteSpace(request.remark) ? $"操作人 {request.operatorId} 手工扣减" : request.remark
        });

    private ObjectResult Error(Exception ex)
    {
        var status = ex is ArgumentException ? 400 : ex is KeyNotFoundException ? 404 : 409;
        return StatusCode(status, ApiResponse<object>.Fail(status, ex.Message));
    }
}
