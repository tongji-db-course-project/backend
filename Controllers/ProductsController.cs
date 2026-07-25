using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// 查询商品列表（分页 + 关键词 + 状态过滤）
    /// </summary>
    /// <param name="page">页码，从 1 开始</param>
    /// <param name="size">每页数量</param>
    /// <param name="keyword">按商品名称或条码模糊匹配</param>
    /// <param name="status">按商品状态精确过滤，如「在售」「停售」</param>
    [HttpGet(Name = "listProducts")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<ProductListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListProducts(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null)
    {
        var result = await _productService.ListProductsAsync(page, size, keyword, status);
        return Ok(ApiResponse<PageResult<ProductListItemDto>>.Ok(result));
    }
}
