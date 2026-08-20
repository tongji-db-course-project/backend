using backend.Dtos;

namespace backend.Services;

public interface IPurchaseOrderService
{
    Task<PageResult<PurchaseOrderDto>> ListOrdersAsync(
        int page, int size, string? keyword, string? status, int? supplierId);

    Task<PurchaseOrderDto> CreateOrderAsync(CreatePurchaseOrderRequest request);

    Task<PurchaseOrderDto> GetOrderAsync(int orderId);

    Task<PurchaseOrderDto> UpdateOrderAsync(int orderId, CreatePurchaseOrderRequest request);

    Task CancelOrderAsync(int orderId);

    Task<OrderStatusResultDto> ApproveOrderAsync(int orderId, ApprovalRequest request);

    Task<OrderStatusResultDto> RejectOrderAsync(int orderId, ApprovalRequest request);

    Task<PurchaseStockInResultDto> StockInAsync(int orderId, PurchaseStockInRequest request);
}
