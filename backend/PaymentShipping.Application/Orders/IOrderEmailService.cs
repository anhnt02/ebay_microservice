using PaymentShipping.Domain.Entities;

namespace PaymentShipping.Application.Orders;

/// <summary>Service gửi email thông báo liên quan đơn hàng.</summary>
public interface IOrderEmailService
{
    Task SendPaymentSuccessEmailAsync(Order order, CancellationToken ct = default);
    Task SendOrderStatusChangedEmailAsync(Order order, string oldStatus, string newStatus, CancellationToken ct = default);
}
