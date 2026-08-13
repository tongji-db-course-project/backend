using backend.Dtos;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

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

    [HttpPost(Name = "createProduct")]
    [ProducesResponseType(typeof(ApiResponse<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] ProductDto dto)
    {
        try
        {
            var result = await _productService.CreateProductAsync(dto);
            return Ok(ApiResponse<Product>.Ok(result));
        }
        catch (BusinessException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpGet("{productId:int}", Name = "getProduct")]
    [ProducesResponseType(typeof(ApiResponse<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(int productId)
    {
        var result = await _productService.GetProductAsync(productId);
        if (result == null)
            return NotFound(ApiResponse<string>.Fail(400, "商品不存在"));

        return Ok(ApiResponse<Product>.Ok(result));
    }

    [HttpPut("{productId:int}", Name = "updateProduct")]
    [ProducesResponseType(typeof(ApiResponse<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(int productId, [FromBody] ProductDto dto)
    {
        try
        {
            var result = await _productService.UpdateProductAsync(productId, dto);
            if (result == null)
                return NotFound(ApiResponse<string>.Fail(400, "商品不存在"));

            return Ok(ApiResponse<Product>.Ok(result));
        }
        catch (BusinessException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Code, ex.Message));
        }
    }

    [HttpDelete("{productId:int}", Name = "deleteProduct")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int productId)
    {
        var success = await _productService.DeleteProductAsync(productId);
        if (!success)
            return NotFound(ApiResponse<string>.Fail(400, "商品不存在"));

        return Ok(ApiResponse<object>.Ok(null!, "删除成功"));
    }

    [HttpGet("barcode/{barcode}", Name = "getProductByBarcode")]
    [ProducesResponseType(typeof(ApiResponse<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductByBarcode(string barcode)
    {
        var result = await _productService.GetProductByBarcodeAsync(barcode);
        if (result == null)
            return NotFound(ApiResponse<string>.Fail(400, "商品不存在"));

        return Ok(ApiResponse<Product>.Ok(result));
    }

    [HttpGet("warning-stock", Name = "listWarningStockProducts")]
    [ProducesResponseType(typeof(ApiResponse<PageResult<ProductListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListWarningStockProducts(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? status = null)
    {
        var result = await _productService.ListWarningStockProductsAsync(page, size, keyword, status);
        return Ok(ApiResponse<PageResult<ProductListItemDto>>.Ok(result));
    }
}
