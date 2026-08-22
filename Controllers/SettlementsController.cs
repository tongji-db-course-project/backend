using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Route("settlements"), Authorize]
public class SettlementsController : ControllerBase
{
    private readonly ISettlementService _service;
    public SettlementsController(ISettlementService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int size = 10, string? keyword = null, string? status = null, int? supplierId = null) =>
        Ok(ApiResponse<PageResult<SettlementDto>>.Ok(await _service.ListAsync(page, size, keyword, status, supplierId)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSettlementRequest request) => await Execute(() => _service.CreateAsync(request));

    [HttpGet("{settlementId:int}")]
    public async Task<IActionResult> Get(int settlementId) => await Execute(() => _service.GetAsync(settlementId));

    [HttpPut("{settlementId:int}/pay")]
    public async Task<IActionResult> Pay(int settlementId, [FromBody] PaySettlementRequest request) =>
        await Execute(() => _service.PayAsync(settlementId, request));

    [HttpGet("/suppliers/{supplierId:int}/settlements")]
    public async Task<IActionResult> SupplierSettlements(int supplierId, int page = 1, int size = 10) =>
        Ok(ApiResponse<PageResult<SettlementDto>>.Ok(await _service.ListAsync(page, size, null, null, supplierId)));

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(ApiResponse<T>.Ok(await action())); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(400, ex.Message)); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(404, ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.Fail(409, ex.Message)); }
    }
}
