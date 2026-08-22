using Microsoft.AspNetCore.Mvc;
using PaymentShipping.Application.Auth;
using PaymentShipping.Contracts;
using PaymentShipping.Contracts.Auth;

namespace PaymentShipping.Api.Controllers;

public sealed class AuthController : BaseController
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(req, ct);
        return Ok(ApiResponse<AuthResultDto>.Ok(result, CurrentCorrelationId, "Registration successful"));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req, ct);
        return Ok(ApiResponse<AuthResultDto>.Ok(result, CurrentCorrelationId, "Login successful"));
    }

    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var userId = CurrentUserId;
        return Ok(ApiResponse<object>.Ok(new { id = userId, username = User.Identity?.Name ?? "user", email = "user@example.com", role = "buyer" }, CurrentCorrelationId, "User fetched"));
    }

    [HttpPost("logout")]
    public IActionResult Logout() => Ok(ApiResponse<object>.Ok(new { }, CurrentCorrelationId, "Logged out"));
}
