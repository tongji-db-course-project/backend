using backend.Dtos;

namespace backend.Services;

public interface ISaleService
{
    Task<PageResult<SaleListItemDto>> ListAsync(int page, int size, string? keyword, string? status, DateTime? startDate, DateTime? endDate);
    Task<SaleDetailDto> GetAsync(int saleId);
    Task<SaleDetailDto> CreateAsync(CreateSaleRequest request, int userId);
    Task CancelAsync(int saleId, int operatorId);
    Task<IReadOnlyList<OrderStatusLogDto>> GetTimelineAsync(int saleId);
}
