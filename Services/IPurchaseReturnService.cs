using backend.Dtos;

namespace backend.Services;

public interface IPurchaseReturnService
{
    Task<PageResult<PurchaseReturnDto>> ListAsync(int page, int size, string? keyword, string? status,
        int? supplierId, int? purchaseId, DateTime? startDate, DateTime? endDate);
    Task<PurchaseReturnDto> GetAsync(int returnId);
    Task<PurchaseReturnDto> CreateAsync(SavePurchaseReturnRequest request, int operatorId);
    Task<PurchaseReturnDto> UpdateAsync(int returnId, SavePurchaseReturnRequest request, int operatorId);
    Task<PurchaseReturnDto> ApproveAsync(int returnId, PurchaseReturnApprovalRequest request, int approverId);
    Task<PurchaseReturnDto> CompleteAsync(int returnId, CompletePurchaseReturnRequest request, int operatorId);
    Task CancelAsync(int returnId, int operatorId);
    Task<IReadOnlyList<OrderStatusLogDto>> GetTimelineAsync(int returnId);
}
