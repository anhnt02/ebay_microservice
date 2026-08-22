namespace PaymentShipping.Api.Middleware;

/// <summary>
/// 🔐 Kiểm tra X-Payment-Secret-Key header cho các payment endpoints.
/// Bảo vệ API thanh toán khỏi truy cập trái phép.
/// </summary>
public sealed class PaymentSecretKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PaymentSecretKeyMiddleware> _logger;
    private readonly string _secretKey;

    // Paths cần kiểm tra secret key
    private static readonly string[] ProtectedPaths =
    [
        "/api/payments/capture",
        "/api/payments/confirm-cod"
    ];

    public PaymentSecretKeyMiddleware(
        RequestDelegate next,
        ILogger<PaymentSecretKeyMiddleware> logger,
        IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _secretKey = config["Payment:SecretKey"]
                     ?? throw new InvalidOperationException("Payment:SecretKey is not configured");
    }

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Chỉ kiểm tra cho protected paths
        if (ProtectedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            var providedKey = context.Request.Headers["X-Payment-Secret-Key"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(providedKey) || providedKey != _secretKey)
            {
                var correlationId = context.Items["X-Correlation-Id"]?.ToString() ?? context.TraceIdentifier;

                _logger.LogWarning(
                    "PaymentSecretKey validation failed | cid={cid} | path={path} | ip={ip}",
                    correlationId, path,
                    context.Connection.RemoteIpAddress?.ToString());

                context.Response.StatusCode  = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    code    = "PAYMENT_KEY_INVALID",
                    message = "Invalid or missing payment secret key",
                    correlationId
                });
                return;
            }
        }

        await _next(context);
    }
}
