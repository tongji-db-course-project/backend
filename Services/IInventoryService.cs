using backend.Dtos;

namespace backend.Services;

public interface IInventoryService
{
    Task<PageResult<InventoryDto>> ListInventoryAsync(
        int page, int size, string? keyword, string? status, int? productId);

    Task<InventoryDto> GetInventoryAsync(int inventoryId);

    Task<InventoryDto> GetInventoryByProductAsync(int productId);

    Task<PageResult<InventoryDto>> ListWarningAsync(
        int page, int size, string? keyword, string? status);

    Task<PageResult<InventoryRecordDto>> ListRecordsAsync(
        int page, int size, string? keyword, int? productId, string? recordType);

    Task<InventoryDto> AdjustInventoryAsync(
        InventoryAdjustDto request, int operatorId);

    Task<IReadOnlyList<SupplierPurchaseSuggestionDto>> GetPurchaseSuggestionsAsync();
}
