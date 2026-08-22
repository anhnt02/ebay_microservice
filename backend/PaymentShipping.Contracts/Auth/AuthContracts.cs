namespace PaymentShipping.Contracts.Auth;

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? FullName,
    string? Phone
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResultDto(
    int UserId,
    string Username,
    string Email,
    string Token,
    string AccessToken,
    DateTime ExpiresAt
);
