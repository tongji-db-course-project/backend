using backend.Dtos;

namespace backend.Services;

public interface ISupplierService
{
    Task<PageResult<SupplierDto>> ListAsync(int page, int size, string? keyword, string? status, string? creditLevel);
    Task<SupplierDto> GetAsync(int supplierId);
    Task<SupplierDto> CreateAsync(SaveSupplierRequest request);
    Task<SupplierDto> UpdateAsync(int supplierId, SaveSupplierRequest request);
    Task DeleteAsync(int supplierId);
    Task<PageResult<ProductListItemDto>> ListProductsAsync(int supplierId, int page, int size);
    Task<SupplierPerformanceDto> GetPerformanceAsync(int supplierId, bool updateCreditLevel);
}
