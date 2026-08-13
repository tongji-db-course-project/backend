using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet(Name = "listCategories")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<Category>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCategories(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null)
    {
        var result = await _categoryService.ListCategoriesAsync(page, size, keyword, status);
        return Ok(ApiResponse<PageResult<Category>>.Ok(result));
    }

    [HttpPost(Name = "createCategory")]
    [ProducesResponseType(typeof(ApiResponse<Category>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryDto dto)
    {
        try
        {
            var result = await _categoryService.CreateCategoryAsync(dto);
            return Ok(ApiResponse<Category>.Ok(result));
        }
        catch (BusinessException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpGet("{categoryId:int}", Name = "getCategory")]
    [ProducesResponseType(typeof(ApiResponse<Category>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(int categoryId)
    {
        var result = await _categoryService.GetCategoryAsync(categoryId);
        if (result == null)
            return NotFound(ApiResponse<string>.Fail(400, "商品分类不存在"));

        return Ok(ApiResponse<Category>.Ok(result));
    }

    [HttpPut("{categoryId:int}", Name = "updateCategory")]
    [ProducesResponseType(typeof(ApiResponse<Category>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int categoryId, [FromBody] CategoryDto dto)
    {
        try
        {
            var result = await _categoryService.UpdateCategoryAsync(categoryId, dto);
            if (result == null)
                return NotFound(ApiResponse<string>.Fail(400, "商品分类不存在"));

            return Ok(ApiResponse<Category>.Ok(result));
        }
        catch (BusinessException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpDelete("{categoryId:int}", Name = "deleteCategory")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        var success = await _categoryService.DeleteCategoryAsync(categoryId);
        if (!success)
            return NotFound(ApiResponse<string>.Fail(400, "商品分类不存在"));

        return Ok(ApiResponse<object>.Ok(null!, "删除成功"));
    }
}
