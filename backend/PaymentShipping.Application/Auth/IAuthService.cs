using PaymentShipping.Contracts.Auth;

namespace PaymentShipping.Application.Auth;

/// <summary>Service xử lý đăng ký và đăng nhập user.</summary>
public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResultDto> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
