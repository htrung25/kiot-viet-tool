using KiotVietTool.Application.Interfaces;
using KiotVietTool.Application.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KiotVietTool.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<UserSessionService>();
        services.AddSingleton<IUserSessionService>(sp => sp.GetRequiredService<UserSessionService>());
        services.AddSingleton<PendingSignInService>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddSingleton<ITelegramService, TelegramService>();
        services.AddTransient<IKiotVietConnectionService, KiotVietConnectionService>();
        services.AddSingleton<ICatalogSyncService, CatalogSyncService>();
        services.AddTransient<ICatalogService, CatalogService>();
        services.AddTransient<IDiscountProgramService, DiscountProgramService>();
        services.AddTransient<ProgramScheduleService>();
        services.AddSingleton<IPriceDeploymentService, PriceDeploymentService>();
        return services;
    }
}
