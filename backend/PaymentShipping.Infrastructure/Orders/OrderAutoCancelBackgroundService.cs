using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentShipping.Application.Common;
using PaymentShipping.Application.Orders;
using PaymentShipping.Domain.Entities;
using PaymentShipping.Infrastructure.Persistence;

namespace PaymentShipping.Infrastructure.Orders;

/// <summary>
/// Background service tự động huỷ đơn hàng quá thời gian chờ thanh toán.
/// Mỗi phút chạy 1 lần, huỷ các đơn PendingPayment quá 30 phút.
/// </summary>
public sealed class OrderAutoCancelBackgroundService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<OrderAutoCancelBackgroundService> _logger;
    private const int PaymentTimeoutMinutes = 30;

    public OrderAutoCancelBackgroundService(
        IServiceProvider sp,
        ILogger<OrderAutoCancelBackgroundService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderAutoCancel background service started. Timeout={min}min", PaymentTimeoutMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CancelTimedOutOrdersAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error in OrderAutoCancel service");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CancelTimedOutOrdersAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IOrderEmailService>();

        var cutoff = DateTime.UtcNow.AddMinutes(-PaymentTimeoutMinutes);

        var timedOut = await db.Orders
            .Include(o => o.Buyer)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Payments)
            .Where(o => o.Status == OrderStatuses.PendingPayment && o.OrderDate < cutoff)
            .ToListAsync(ct);

        if (!timedOut.Any()) return;

        _logger.LogInformation("AutoCancel: found {count} timed-out orders", timedOut.Count);

        foreach (var order in timedOut)
        {
            var oldStatus = order.Status;
            order.Status = OrderStatuses.Cancelled;

            // Restore stock
            foreach (var item in order.OrderItems)
            {
                if (item.Product != null)
                {
                    item.Product.StockQuantity += item.Quantity;
                    item.Product.Status = "active";
                }
            }

            foreach (var payment in order.Payments)
                payment.Status = PaymentStatuses.Cancelled;

            _logger.LogInformation(
                "AutoCancel: order #{orderId} cancelled (payment timeout)",
                order.Id);

            try
            {
                await emailService.SendOrderStatusChangedEmailAsync(order, oldStatus, order.Status, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AutoCancel: failed to send email for order #{orderId}", order.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
