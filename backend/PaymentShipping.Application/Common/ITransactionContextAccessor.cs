namespace PaymentShipping.Application.Common;

/// <summary>
/// Truy cập transaction context (correlation ID, transaction ID) 
/// để đính kèm vào log.
/// </summary>
public interface ITransactionContextAccessor
{
    string? CorrelationId { get; set; }
    string? TransactionId { get; set; }
}
