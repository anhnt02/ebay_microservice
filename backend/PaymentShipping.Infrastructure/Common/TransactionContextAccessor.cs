using PaymentShipping.Application.Common;

namespace PaymentShipping.Infrastructure.Common;

/// <summary>
/// Lưu trữ CorrelationId và TransactionId trong scope của HTTP request.
/// </summary>
public sealed class TransactionContextAccessor : ITransactionContextAccessor
{
    public string? CorrelationId { get; set; }
    public string? TransactionId { get; set; }
}
