using KiotVietTool.Application.Features.Auth;
using KiotVietTool.Application.Features.Customers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KiotVietTool.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddTransient<ICustomerService, CustomerService>();

        services.AddSingleton<UserSession>();
        services.AddSingleton<IUserSession>(sp => sp.GetRequiredService<UserSession>());
        services.AddTransient<IAuthService, AuthService>();
        return services;
    }
}
