using PaymentShipping.Application.Common;
using PaymentShipping.Application.Payments;
using Microsoft.Extensions.Logging;

namespace PaymentShipping.Infrastructure.Payments;

/// <summary>
/// COD (Cash on Delivery) provider — xác nhận ngay lập tức.
/// 🔐 Yêu cầu X-Payment-Secret-Key header để bảo vệ endpoint.
/// </summary>
public sealed class CodPaymentProvider : IPaymentProvider
{
    public string ProviderName => "cod";

    private readonly ILogger<CodPaymentProvider> _logger;
    private readonly ITransactionContextAccessor _txContext;

    public CodPaymentProvider(ILogger<CodPaymentProvider> logger, ITransactionContextAccessor txContext)
    {
        _logger = logger;
        _txContext = txContext;
    }

    public Task<PaymentProviderResult> InitiateAsync(PaymentRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "COD InitiatePayment | cid={cid} | tx={tx} | orderId={orderId} | amount={amount}",
            _txContext.CorrelationId, _txContext.TransactionId,
            request.OrderId, request.Amount);

        var txId = $"COD-{request.OrderId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var result = new PaymentProviderResult(
            Success: true,
            TransactionId: txId,
            Status: "PENDING_DELIVERY",
            Message: "COD payment initiated. Payment will be collected upon delivery.",
            ProviderRawResponse: $"{{\"type\":\"cod\",\"transactionId\":\"{txId}\",\"status\":\"PENDING_DELIVERY\"}}");

        return Task.FromResult(result);
    }

    /// <summary>Confirm COD khi giao hàng thành công.</summary>
    public Task<PaymentProviderResult> CaptureAsync(string providerOrderId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "COD CapturePayment | cid={cid} | tx={tx} | txId={txId}",
            _txContext.CorrelationId, _txContext.TransactionId, providerOrderId);

        var result = new PaymentProviderResult(
            Success: true,
            TransactionId: providerOrderId,
            Status: "CAPTURED",
            Message: "COD payment captured upon delivery.",
            ProviderRawResponse: $"{{\"type\":\"cod\",\"transactionId\":\"{providerOrderId}\",\"status\":\"CAPTURED\"}}");

        return Task.FromResult(result);
    }
}
