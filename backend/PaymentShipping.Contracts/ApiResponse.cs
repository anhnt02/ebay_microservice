namespace PaymentShipping.Contracts;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Code { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public string? CorrelationId { get; init; }

    private ApiResponse() { }

    public static ApiResponse<T> Ok(T data, string? correlationId = null, string? message = null) =>
        new()
        {
            Success = true,
            Code = "OK",
            Message = message,
            Data = data,
            CorrelationId = correlationId
        };

    public static ApiResponse<T> Fail(string message, string code = "ERROR", string? correlationId = null) =>
        new()
        {
            Success = false,
            Code = code,
            Message = message,
            CorrelationId = correlationId
        };
}
