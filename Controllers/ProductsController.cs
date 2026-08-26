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
        [FromQuery] string? status = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] int? minStock = null,
        [FromQuery] int? maxStock = null)
    {
        var result = await _productService.ListProductsAsync(
            page, size, keyword, status, categoryId, supplierId, minStock, maxStock);
        return Ok(ApiResponse<PageResult<ProductListItemDto>>.Ok(result));
    }

    /// <summary>
    /// 根据 ID 查询商品详情
    /// </summary>
    [HttpGet("{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(int productId)
    {
        var result = await _productService.GetProductByIdAsync(productId);

        if (result == null)
            return NotFound(ApiResponse<string>.Fail(404, "商品不存在"));

        return Ok(ApiResponse<ProductListItemDto>.Ok(result));
    }

    /// <summary>
    /// 根据条码查询商品
    /// </summary>
    [HttpGet("barcode/{barcode}")]
    [ProducesResponseType(typeof(ApiResponse<ProductListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductByBarcode(string barcode)
    {
        var result = await _productService.GetProductByBarcodeAsync(barcode);

        if (result == null)
            return NotFound(ApiResponse<string>.Fail(404, "商品不存在"));

        return Ok(ApiResponse<ProductListItemDto>.Ok(result));
    }

    /// <summary>
    /// 查询库存低于预警线的商品（分页）
    /// </summary>
    [HttpGet("warning-stock")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<ProductListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarningStockProducts(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        var result = await _productService.GetWarningStockProductsAsync(page, size);
        return Ok(ApiResponse<PageResult<ProductListItemDto>>.Ok(result));
    }

    /// <summary>
    /// 新增商品
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] ProductDto dto)
    {
        try
        {
            var result = await _productService.CreateProductAsync(dto);
            return Ok(ApiResponse<ProductListItemDto>.Ok(result));
        }
        catch (BusinessException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Code, ex.Message));
        }
    }

    /// <summary>
    /// 修改商品信息
    /// </summary>
    [HttpPut("{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProduct(int productId, [FromBody] ProductDto dto)
    {
        try
        {
            var result = await _productService.UpdateProductAsync(productId, dto);

            if (result == null)
                return NotFound(ApiResponse<string>.Fail(404, "商品不存在"));

            return Ok(ApiResponse<ProductListItemDto>.Ok(result));
        }
        catch (BusinessException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Code, ex.Message));
        }
    }

    /// <summary>
    /// 逻辑删除商品（改为「停售」）
    /// </summary>
    [HttpDelete("{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int productId)
    {
        var success = await _productService.DeleteProductAsync(productId);

        if (!success)
            return NotFound(ApiResponse<string>.Fail(404, "商品不存在"));

        return Ok(ApiResponse<string>.Ok("删除成功"));
    }
}
