using Microsoft.Extensions.DependencyInjection;

namespace PaymentShipping.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Application layer không cần đăng ký gì thêm ở đây
        // (interfaces được đăng ký ở Infrastructure layer)
        return services;
    }
}
