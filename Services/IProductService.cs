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
}
