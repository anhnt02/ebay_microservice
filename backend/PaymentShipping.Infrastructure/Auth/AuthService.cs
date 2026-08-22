using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PaymentShipping.Application.Auth;
using PaymentShipping.Application.Common;
using PaymentShipping.Contracts.Auth;
using PaymentShipping.Domain.Entities;
using PaymentShipping.Domain.Exceptions;
using PaymentShipping.Infrastructure.Persistence;

namespace PaymentShipping.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;
    private readonly ITransactionContextAccessor _txContext;
    private readonly PaymentShipping.Application.Notifications.IEmailService _email;

    public AuthService(
        AppDbContext db,
        IConfiguration config,
        ILogger<AuthService> logger,
        ITransactionContextAccessor txContext,
        PaymentShipping.Application.Notifications.IEmailService email)
    {
        _db = db;
        _config = config;
        _logger = logger;
        _txContext = txContext;
        _email = email;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Register started | cid={cid} | tx={tx} | email={email}",
            _txContext.CorrelationId, _txContext.TransactionId, request.Email);

        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (exists)
            throw new ConflictException("Email already registered", "EMAIL_ALREADY_EXISTS");

        var usernameExists = await _db.Users.AnyAsync(u => u.Username == request.Username, ct);
        if (usernameExists)
            throw new ConflictException("Username already taken", "USERNAME_ALREADY_EXISTS");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Register succeeded | cid={cid} | tx={tx} | userId={userId}",
            _txContext.CorrelationId, _txContext.TransactionId, user.Id);

        await _email.SendAsync(
            user.Email,
            "🎉 Welcome to CloneEbay - Account Created Successfully!",
            $"<h2>Hello {user.Username},</h2><p>Your account has been created successfully on CloneEbay!</p><p>You can now log in and explore our Payment & Shipping features.</p><p>Best regards,<br/><strong>CloneEbay Microservices Team</strong></p>",
            default); // use default to avoid cancellation on exit

        var (token, expiresAt) = GenerateToken(user);
        return new AuthResultDto(user.Id, user.Username!, user.Email!, token, token, expiresAt);
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Login started | cid={cid} | tx={tx} | email={email}",
            _txContext.CorrelationId, _txContext.TransactionId, request.Email);

        var emailOrUser = (request.Email ?? "").Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => 
            u.Email.ToLower() == emailOrUser.ToLower() || 
            (u.Username != null && u.Username.ToLower() == emailOrUser.ToLower()), ct);

        if (user == null)
        {
            user = new User
            {
                Username = emailOrUser.Contains("@") ? emailOrUser.Split('@')[0] : emailOrUser,
                Email = emailOrUser.Contains("@") ? emailOrUser : $"{emailOrUser}@ebay.com",
                FullName = emailOrUser.Contains("@") ? emailOrUser.Split('@')[0] : emailOrUser,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            // Send welcome email on auto-provision
            await _email.SendAsync(
                user.Email,
                "🎉 Welcome to CloneEbay - Account Created Successfully!",
                $"<h2>Hello {user.Username},</h2><p>Your account has been created successfully on CloneEbay!</p><p>You can now log in and explore our Payment & Shipping features.</p><p>Best regards,<br/><strong>CloneEbay Microservices Team</strong></p>",
                default); // use default to avoid cancellation on exit
        }
        else
        {
            user.IsActive = true;
        }

        _logger.LogInformation(
            "Login succeeded | cid={cid} | tx={tx} | userId={userId}",
            _txContext.CorrelationId, _txContext.TransactionId, user.Id);

        var (token, expiresAt) = GenerateToken(user);
        return new AuthResultDto(user.Id, user.Username ?? user.Email!, user.Email!, token, token, expiresAt);
    }

    private (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var jwtKey = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key not configured");

        var issuer   = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var expMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expMinutes);

        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("email", user.Email ?? ""),
            new Claim("username", user.Username ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
