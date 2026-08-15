using backend.Dtos;

namespace backend.Services;

/// <summary>
/// 商品业务接口
/// </summary>
public interface IProductService
{
    /// <summary>
    /// 分页查询商品列表
    /// </summary>
    Task<PageResult<ProductListItemDto>> ListProductsAsync(
        int page, int size, string? keyword, string? status);

    /// <summary>
    /// 根据ID查询商品详情
    /// </summary>
    Task<ProductListItemDto?> GetProductByIdAsync(int productId);

    /// <summary>
    /// 根据条码查询商品
    /// </summary>
    Task<ProductListItemDto?> GetProductByBarcodeAsync(string barcode);

    /// <summary>
    /// 查询库存低于预警线的商品
    /// </summary>
    Task<PageResult<ProductListItemDto>> GetWarningStockProductsAsync(int page, int size);

    /// <summary>
    /// 新增商品
    /// </summary>
    Task<ProductListItemDto> CreateProductAsync(ProductDto dto);

    /// <summary>
    /// 修改商品信息
    /// </summary>
    Task<ProductListItemDto?> UpdateProductAsync(int productId, ProductDto dto);

    /// <summary>
    /// 逻辑删除商品（改为停售）
    /// </summary>
    Task<bool> DeleteProductAsync(int productId);
}