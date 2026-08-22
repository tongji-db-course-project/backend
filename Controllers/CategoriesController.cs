using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;

    public CategoriesController(ICategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null)
    {
        var result = await _service.ListCategoriesAsync(page, size, keyword, status);
        return Ok(ApiResponse<PageResult<Category>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryDto request)
    {
        try { return Ok(ApiResponse<Category>.Ok(await _service.CreateCategoryAsync(request))); }
        catch (BusinessException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Code, ex.Message)); }
    }

    [HttpGet("{categoryId:int}")]
    public async Task<IActionResult> Get(int categoryId)
    {
        var result = await _service.GetCategoryAsync(categoryId);
        return result is null
            ? NotFound(ApiResponse<object>.Fail(404, "商品分类不存在"))
            : Ok(ApiResponse<Category>.Ok(result));
    }

    [HttpPut("{categoryId:int}")]
    public async Task<IActionResult> Update(int categoryId, [FromBody] CategoryDto request)
    {
        try
        {
            var result = await _service.UpdateCategoryAsync(categoryId, request);
            return result is null
                ? NotFound(ApiResponse<object>.Fail(404, "商品分类不存在"))
                : Ok(ApiResponse<Category>.Ok(result));
        }
        catch (BusinessException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Code, ex.Message)); }
    }

    [HttpDelete("{categoryId:int}")]
    public async Task<IActionResult> Delete(int categoryId)
    {
        return await _service.DeleteCategoryAsync(categoryId)
            ? Ok(ApiResponse<object?>.Ok(null, "删除成功"))
            : NotFound(ApiResponse<object>.Fail(404, "商品分类不存在"));
    }
}
