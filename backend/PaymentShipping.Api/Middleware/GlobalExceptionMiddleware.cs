using System.Net;
using System.Text.Json;
using PaymentShipping.Contracts;
using PaymentShipping.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace PaymentShipping.Api.Middleware;

/// <summary>
/// Xử lý tất cả exception chưa được bắt, trả về ApiResponse chuẩn.
/// 🐞 Log chi tiết lỗi với correlation ID.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["X-Correlation-Id"]?.ToString()
                                ?? context.TraceIdentifier;
            var transactionId = context.Items["X-Transaction-Id"]?.ToString()
                                ?? correlationId;

            _logger.LogError(ex,
                "Unhandled error | cid={cid} | tx={tx} | path={path} | method={method}",
                correlationId, transactionId,
                context.Request.Path, context.Request.Method);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started, cannot write error | cid={cid}", correlationId);
                throw;
            }

            await WriteErrorAsync(context, ex, correlationId, transactionId);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception ex, string cid, string txId)
    {
        int statusCode;
        string code;
        string message;

        switch (ex)
        {
            case AppException appEx:
                statusCode = appEx.StatusCode;
                code       = appEx.Code;
                message    = appEx.Message;
                break;

            case BadHttpRequestException:
                statusCode = (int)HttpStatusCode.BadRequest;
                code       = "BAD_REQUEST";
                message    = ex.Message;
                break;

            case DbUpdateException:
                statusCode = (int)HttpStatusCode.Conflict;
                code       = "DB_CONFLICT";
                message    = "A database conflict occurred.";
                break;

            case OperationCanceledException:
                statusCode = 408;
                code       = "REQUEST_TIMEOUT";
                message    = "The request timed out.";
                break;

            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                code       = "INTERNAL_ERROR";
                message    = $"An unexpected error occurred: {ex.Message}";
                break;
        }

        context.Response.StatusCode    = statusCode;
        context.Response.ContentType   = "application/json";
        context.Response.Headers["X-Correlation-Id"] = cid;
        context.Response.Headers["X-Transaction-Id"] = txId;

        var payload = ApiResponse<object>.Fail(message, code, cid);
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOpts));
    }
}
