using backend.Dtos;

namespace backend.Services;

public interface IProductService
{
    Task<PageResult<ProductListItemDto>> ListProductsAsync(
        int page, int size, string? keyword, string? status);

    Task<Product> CreateProductAsync(ProductDto dto);

    Task<Product?> GetProductAsync(int productId);

    Task<Product?> UpdateProductAsync(int productId, ProductDto dto);

    Task<bool> DeleteProductAsync(int productId);

    Task<Product?> GetProductByBarcodeAsync(string barcode);

    Task<PageResult<ProductListItemDto>> ListWarningStockProductsAsync(
        int page, int size, string? keyword, string? status);
}
