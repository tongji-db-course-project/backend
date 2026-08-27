using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Route("suppliers"), Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _service;
    public SuppliersController(ISupplierService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(
        int page = 1,
        int size = 10,
        string? keyword = null,
        string? status = null,
        string? creditLevel = null) =>
        Ok(ApiResponse<PageResult<SupplierDto>>.Ok(
            await _service.ListAsync(page, size, keyword, status, creditLevel)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveSupplierRequest request) =>
        await Execute(() => _service.CreateAsync(request));

    [HttpGet("{supplierId:int}")]
    public async Task<IActionResult> Get(int supplierId) => await Execute(() => _service.GetAsync(supplierId));

    [HttpPut("{supplierId:int}")]
    public async Task<IActionResult> Update(int supplierId, [FromBody] SaveSupplierRequest request) =>
        await Execute(() => _service.UpdateAsync(supplierId, request));

    [HttpDelete("{supplierId:int}")]
    public async Task<IActionResult> Delete(int supplierId)
    {
        try { await _service.DeleteAsync(supplierId); return Ok(ApiResponse<object?>.Ok(null, "删除成功")); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(404, ex.Message)); }
    }

    [HttpGet("{supplierId:int}/products")]
    public async Task<IActionResult> Products(int supplierId, int page = 1, int size = 10)
    {
        try { return Ok(ApiResponse<PageResult<ProductListItemDto>>.Ok(await _service.ListProductsAsync(supplierId, page, size))); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(404, ex.Message)); }
    }

    [HttpGet("{supplierId:int}/performance")]
    public async Task<IActionResult> Performance(int supplierId, bool updateCreditLevel = false) =>
        await Execute(() => _service.GetPerformanceAsync(supplierId, updateCreditLevel));

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(ApiResponse<T>.Ok(await action())); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(400, ex.Message)); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(404, ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.Fail(409, ex.Message)); }
    }
}
