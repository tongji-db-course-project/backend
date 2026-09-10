using backend.Dtos;

namespace backend.Services;

public interface IStockCheckService
{
    Task<PageResult<StockCheckListItemDto>> ListAsync(int page, int size, string? status);
    Task<StockCheckDetailDto> GetAsync(int checkId);
    Task<StockCheckDetailDto> CreateAsync(CreateStockCheckRequest request, int operatorId);
    Task<StockCheckDetailDto> ConfirmAsync(int checkId, ConfirmStockCheckRequest request, int operatorId);
    Task CancelAsync(int checkId, int operatorId);
}
