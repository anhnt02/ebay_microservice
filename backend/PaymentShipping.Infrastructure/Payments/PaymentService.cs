using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentShipping.Application.Common;
using PaymentShipping.Application.Orders;
using PaymentShipping.Application.Payments;
using PaymentShipping.Contracts.Payments;
using PaymentShipping.Domain.Entities;
using PaymentShipping.Domain.Exceptions;
using PaymentShipping.Infrastructure.Persistence;

namespace PaymentShipping.Infrastructure.Payments;

/// <summary>
/// Điều phối thanh toán — chọn đúng IPaymentProvider và ghi log transaction.
/// ⚡ Đảm bảo xác nhận thanh toán ≤ 2 giây.
/// 🐞 Log chi tiết transaction ID và lỗi.
/// </summary>
public sealed class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly IOrderEmailService _emailService;
    private readonly ILogger<PaymentService> _logger;
    private readonly ITransactionContextAccessor _txContext;

    public PaymentService(
        AppDbContext db,
        IEnumerable<IPaymentProvider> providers,
        IOrderEmailService emailService,
        ILogger<PaymentService> logger,
        ITransactionContextAccessor txContext)
    {
        _db = db;
        _providers = providers;
        _emailService = emailService;
        _logger = logger;
        _txContext = txContext;
    }

    /// <summary>Khởi tạo thanh toán (PayPal → trả về approve URL; COD → xác nhận ngay).</summary>
    public async Task<PaymentResultDto> ProcessAsync(int buyerId, ProcessPaymentRequest req, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation(
            "Payment.Process started | cid={cid} | tx={tx} | buyerId={buyer} | orderId={order} | provider={prov}",
            _txContext.CorrelationId, _txContext.TransactionId,
            buyerId, req.OrderId, req.Provider);

        var order = await LoadOrderAsync(req.OrderId, buyerId, ct);
        var payment = GetPendingPayment(order, req.Provider);
        var provider = GetProvider(req.Provider);

        // ⚡ Use CancellationTokenSource with 2s timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        PaymentProviderResult result;
        try
        {
            result = await provider.InitiateAsync(
                new PaymentRequest(
                    OrderId: order.Id,
                    Amount: payment.Amount,
                    Currency: payment.Currency,
                    BuyerEmail: order.Buyer?.Email ?? "",
                    OrderDescription: $"Order #{order.Id}"),
                cts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(
                "Payment timeout >2s | cid={cid} | tx={tx} | orderId={id}",
                _txContext.CorrelationId, _txContext.TransactionId, req.OrderId);
            throw new ValidationException("Payment timed out. Please try again.", "PAYMENT_TIMEOUT");
        }

        sw.Stop();
        _logger.LogInformation(
            "Payment.Process done | cid={cid} | tx={tx} | orderId={id} | success={ok} | elapsed={ms}ms",
            _txContext.CorrelationId, _txContext.TransactionId,
            req.OrderId, result.Success, sw.ElapsedMilliseconds);

        if (!result.Success)
            return new PaymentResultDto(false, "", "FAILED", result.Message);

        // For COD — auto-mark as captured
        if (req.Provider.Equals("cod", StringComparison.OrdinalIgnoreCase))
        {
            await MarkCapturedAsync(order, payment, result.TransactionId, result.ProviderRawResponse, ct);
            return new PaymentResultDto(true, result.TransactionId, "CAPTURED", result.Message);
        }

        // For PayPal — update transactionId, return approve URL
        payment.TransactionId = result.TransactionId;
        payment.ProviderRawResponse = result.ProviderRawResponse;
        await _db.SaveChangesAsync(ct);

        return new PaymentResultDto(
            true, result.TransactionId, "CREATED",
            result.Message, result.ApproveUrl, result.ProviderRawResponse);
    }

    /// <summary>Capture PayPal sau khi user approve trên PayPal.</summary>
    public async Task<PaymentResultDto> CapturePayPalAsync(
        int buyerId, int orderId, string paypalOrderId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Payment.CapturePayPal | cid={cid} | tx={tx} | buyerId={buyer} | orderId={id} | paypalOrderId={pid}",
            _txContext.CorrelationId, _txContext.TransactionId,
            buyerId, orderId, paypalOrderId);

        if (string.IsNullOrWhiteSpace(paypalOrderId))
            throw new ValidationException("PayPal order ID is required", "PAYPAL_ORDER_ID_REQUIRED");

        var order = await LoadOrderAsync(orderId, buyerId, ct);
        var payment = GetPendingPayment(order, "paypal");

        // Allow re-capture if already captured
        if (payment.Status == PaymentStatuses.Captured)
        {
            _logger.LogInformation("Payment already captured | orderId={id}", orderId);
            return new PaymentResultDto(true, payment.TransactionId ?? "", "CAPTURED", "Already captured");
        }

        var provider = GetProvider("paypal");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        PaymentProviderResult result;
        try
        {
            result = await provider.CaptureAsync(paypalOrderId, cts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ValidationException("PayPal capture timed out", "PAYMENT_TIMEOUT");
        }

        _logger.LogInformation(
            "Payment.CapturePayPal result | cid={cid} | tx={tx} | success={ok} | status={status}",
            _txContext.CorrelationId, _txContext.TransactionId, result.Success, result.Status);

        if (!result.Success)
            return new PaymentResultDto(false, "", "FAILED", result.Message);

        await MarkCapturedAsync(order, payment, paypalOrderId, result.ProviderRawResponse, ct);

        return new PaymentResultDto(true, paypalOrderId, "CAPTURED", "PayPal payment captured successfully");
    }

    /// <summary>Confirm COD khi giao hàng.</summary>
    public async Task<PaymentResultDto> ConfirmCodAsync(int buyerId, int orderId, CancellationToken ct = default)
    {
        var order = await LoadOrderAsync(orderId, buyerId, ct);
        var payment = GetPendingPayment(order, "cod");

        if (payment.Status == PaymentStatuses.Captured)
            return new PaymentResultDto(true, payment.TransactionId ?? "", "CAPTURED", "Already captured");

        await MarkCapturedAsync(order, payment, payment.TransactionId ?? $"COD-{orderId}", null, ct);

        return new PaymentResultDto(true, payment.TransactionId!, "CAPTURED", "COD payment confirmed");
    }

    // ── Private helpers ───────────────────────────────────────────────

    private async Task MarkCapturedAsync(
        Order order, Payment payment, string transactionId, string? rawResponse, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        payment.Status = PaymentStatuses.Captured;
        payment.TransactionId = transactionId;
        payment.ProviderRawResponse = rawResponse?[..Math.Min(rawResponse.Length, 4000)];
        payment.PaidAt = DateTime.UtcNow;

        order.Status = OrderStatuses.Paid;
        order.PaidAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation(
            "Payment captured | cid={cid} | tx={tx} | orderId={id} | transactionId={tid}",
            _txContext.CorrelationId, _txContext.TransactionId,
            order.Id, transactionId);

        try
        {
            await _emailService.SendPaymentSuccessEmailAsync(order, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment success email | orderId={id}", order.Id);
        }
    }

    private async Task<Order> LoadOrderAsync(int orderId, int buyerId, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Address)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.BuyerId != buyerId)
            throw new ForbiddenException("You don't have access to this order", "ORDER_FORBIDDEN");

        if (order.Status == OrderStatuses.Cancelled)
            throw new ValidationException("Order is cancelled", "ORDER_ALREADY_CANCELLED");

        return order;
    }

    private static Payment GetPendingPayment(Order order, string provider)
    {
        var payment = order.Payments
            .OrderByDescending(p => p.Id)
            .FirstOrDefault();

        if (payment == null)
            throw new NotFoundException("Payment record not found", "PAYMENT_NOT_FOUND");

        if (!payment.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(
                $"Order payment method is '{payment.Provider}', not '{provider}'",
                "PAYMENT_METHOD_MISMATCH");

        return payment;
    }

    private IPaymentProvider GetProvider(string providerName)
    {
        var provider = _providers.FirstOrDefault(
            p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
            throw new ValidationException($"Payment provider '{providerName}' is not supported", "PROVIDER_NOT_FOUND");

        return provider;
    }
}
