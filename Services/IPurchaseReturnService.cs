using backend.Dtos;

namespace backend.Services;

public interface IPurchaseReturnService
{
    Task<PageResult<PurchaseReturnDto>> ListAsync(int page, int size, string? keyword, string? status,
        int? supplierId, int? purchaseId, DateTime? startDate, DateTime? endDate);
    Task<PurchaseReturnDto> GetAsync(int returnId);
    Task<PurchaseReturnDto> CreateAsync(SavePurchaseReturnRequest request);
    Task<PurchaseReturnDto> UpdateAsync(int returnId, SavePurchaseReturnRequest request);
    Task<PurchaseReturnDto> ApproveAsync(int returnId, ApprovalRequest request);
    Task<PurchaseReturnDto> CompleteAsync(int returnId, CompletePurchaseReturnRequest request);
    Task CancelAsync(int returnId);
    Task<IReadOnlyList<OrderStatusLogDto>> GetTimelineAsync(int returnId);
}
