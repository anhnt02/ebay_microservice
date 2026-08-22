using PaymentShipping.Contracts.Orders;

namespace PaymentShipping.Application.Orders;

/// <summary>Service quản lý vòng đời đơn hàng.</summary>
public interface IOrderService
{
    Task<OrderDto> CreateAsync(int buyerId, CreateOrderRequest request, CancellationToken ct = default);
    Task<OrderTotalDto> CalculateAsync(int buyerId, CalculateOrderRequest request, CancellationToken ct = default);
    Task<OrderDto> GetByIdAsync(int buyerId, int orderId, CancellationToken ct = default);
    Task<PagedResponse<OrderDto>> GetMyOrdersAsync(int buyerId, int page, int pageSize, CancellationToken ct = default);
    Task<OrderDto> CancelAsync(int buyerId, int orderId, CancellationToken ct = default);
}
