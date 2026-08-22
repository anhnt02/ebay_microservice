using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentShipping.Application.Auth;
using PaymentShipping.Application.Common;
using PaymentShipping.Application.Notifications;
using PaymentShipping.Application.Orders;
using PaymentShipping.Application.Payments;
using PaymentShipping.Application.Shipping;
using PaymentShipping.Infrastructure.Auth;
using PaymentShipping.Infrastructure.Common;
using PaymentShipping.Infrastructure.Email;
using PaymentShipping.Infrastructure.Orders;
using PaymentShipping.Infrastructure.Payments;
using PaymentShipping.Infrastructure.Persistence;
using PaymentShipping.Infrastructure.Shipping;

namespace PaymentShipping.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core ──────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null)));

        // ── Transaction Context ───────────────────────────────────────
        services.AddScoped<ITransactionContextAccessor, TransactionContextAccessor>();

        // ── Auth ──────────────────────────────────────────────────────
        services.AddScoped<IAuthService, AuthService>();

        // ── Email ─────────────────────────────────────────────────────
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IOrderEmailService, OrderEmailService>();

        // ── Orders ────────────────────────────────────────────────────
        services.AddScoped<IOrderService, OrderService>();

        // ── Payment Providers (plug-in pattern) ───────────────────────
        // 🧱 Thêm provider mới chỉ cần thêm 1 dòng AddScoped ở đây
        services.AddScoped<IPaymentProvider, SimulatedPayPalProvider>();
        services.AddScoped<IPaymentProvider, CodPaymentProvider>();

        // PayPal provider dùng HttpClient với timeout 2 giây
        services.AddHttpClient<SimulatedPayPalProvider>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(2); // ⚡ 2-second timeout
        });

        // Payment service điều phối
        services.AddScoped<IPaymentService, PaymentService>();

        // ── Shipping ──────────────────────────────────────────────────
        // Simulated shipping client với Polly retry
        services.AddSingleton<SimulatedShippingApiClient>();
        services.AddScoped<IShippingService, ShippingService>();

        // ── Background Services ───────────────────────────────────────
        // Tự động huỷ đơn quá thời gian chờ thanh toán
        services.AddHostedService<OrderAutoCancelBackgroundService>();

        return services;
    }
}
