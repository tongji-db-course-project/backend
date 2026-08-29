using backend.Dtos;

namespace backend.Services;

public interface IReturnService
{
    Task<PageResult<ReturnOrderDto>> ListAsync(int page, int size, string? keyword, string? status);
    Task<ReturnOrderDto> GetAsync(int returnId);
    Task<ReturnOrderDto> CreateAsync(CreateReturnRequest request);
    Task<ReturnOrderDto> ConfirmAsync(int returnId);
    // 注意：合并后保留「拆分参数」契约（与 ReturnsController 最终选择的调用方式对齐），
    //       不把 RejectReturnRequest 传入 Service，避免 Service 依赖 Controllers 命名空间/DTO 造成耦合。
    Task<ReturnOrderDto> RejectAsync(int returnId, int operatorId, string? remark);
    Task<IReadOnlyList<OrderStatusLogDto>> GetTimelineAsync(int returnId);
}
