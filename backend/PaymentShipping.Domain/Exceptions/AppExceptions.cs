namespace PaymentShipping.Domain.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    protected AppException(string message, string code, int statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message, string code = "NOT_FOUND")
        : base(message, code, 404) { }
}

public class ValidationException : AppException
{
    public ValidationException(string message, string code = "VALIDATION_ERROR")
        : base(message, code, 400) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message, string code = "FORBIDDEN")
        : base(message, code, 403) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message, string code = "UNAUTHORIZED")
        : base(message, code, 401) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message, string code = "CONFLICT")
        : base(message, code, 409) { }
}
