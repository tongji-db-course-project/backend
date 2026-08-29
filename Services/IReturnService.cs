using backend.Dtos;

namespace backend.Services;

public interface IReturnService
{
    Task<PageResult<ReturnOrderDto>> ListAsync(int page, int size, string? keyword, string? status);
    Task<ReturnOrderDto> GetAsync(int returnId);
    Task<ReturnOrderDto> CreateAsync(CreateReturnRequest request);
    Task<ReturnOrderDto> ConfirmAsync(int returnId);
    Task<ReturnOrderDto> RejectAsync(int returnId, int operatorId, string? remark);
    Task<IReadOnlyList<OrderStatusLogDto>> GetTimelineAsync(int returnId);
}
