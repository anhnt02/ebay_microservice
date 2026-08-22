namespace PaymentShipping.Api.Middleware;

/// <summary>
/// Gán CorrelationId và TransactionId vào mỗi HTTP request.
/// Dùng để theo dõi log theo từng request.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationHeader  = "X-Correlation-Id";
    private const string TransactionHeader  = "X-Transaction-Id";

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");

        var transactionId = context.Request.Headers[TransactionHeader].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");

        context.Items[CorrelationHeader] = correlationId;
        context.Items[TransactionHeader] = transactionId;

        // Ghi vào context accessor để services có thể đọc
        var txContext = context.RequestServices
            .GetService<PaymentShipping.Application.Common.ITransactionContextAccessor>();
        if (txContext != null)
        {
            txContext.CorrelationId = correlationId;
            txContext.TransactionId = transactionId;
        }

        context.Response.Headers[CorrelationHeader] = correlationId;
        context.Response.Headers[TransactionHeader]  = transactionId;

        await _next(context);
    }
}
