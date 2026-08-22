using backend.Dtos;

namespace backend.Services;

public interface ISettlementService
{
    Task<PageResult<SettlementDto>> ListAsync(int page, int size, string? keyword, string? status, int? supplierId);
    Task<SettlementDto> GetAsync(int settlementId);
    Task<SettlementDto> CreateAsync(CreateSettlementRequest request);
    Task<SettlementDto> PayAsync(int settlementId, PaySettlementRequest request);
}
