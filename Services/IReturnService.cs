using backend.Dtos;

namespace backend.Services;

public interface IReturnService
{
    Task<PageResult<ReturnOrderDto>> ListAsync(int page, int size, string? keyword, string? status);
    Task<ReturnOrderDto> GetAsync(int returnId);
    Task<ReturnOrderDto> CreateAsync(CreateReturnRequest request, int operatorId);
    Task<ReturnOrderDto> ApproveAsync(int returnId, int approverId);
    Task<ReturnOrderDto> CompleteAsync(int returnId, int operatorId);
    Task<ReturnOrderDto> RejectAsync(int returnId, int operatorId, string? remark);
    Task<IReadOnlyList<OrderStatusLogDto>> GetTimelineAsync(int returnId);
}
