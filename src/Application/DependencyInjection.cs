using KiotVietTool.Application.Interfaces;
using KiotVietTool.Application.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(sp => new ServerClockService(TimeProvider.System, sp.GetRequiredService<ILogger<ServerClockService>>()));
        services.AddSingleton<IServerClockService>(sp => sp.GetRequiredService<ServerClockService>());
        services.TryAddSingleton<TimeProvider>(sp => sp.GetRequiredService<ServerClockService>());

        services.AddSingleton<UserSessionService>();
        services.AddSingleton<IUserSessionService>(sp => sp.GetRequiredService<UserSessionService>());
        services.AddSingleton<PendingSignInService>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddSingleton<ITelegramService, TelegramService>();
        services.AddTransient<IKiotVietConnectionService, KiotVietConnectionService>();
        services.AddSingleton<ICatalogSyncService, CatalogSyncService>();
        services.AddTransient<ICatalogService, CatalogService>();
        services.AddTransient<IDiscountProgramService, DiscountProgramService>();
        services.AddSingleton<IDiscountFeedService, DiscountFeedService>();
        services.AddTransient<IDiscountFeedConnectionService, DiscountFeedConnectionService>();
        services.AddSingleton<IInvoiceReconciliationService, InvoiceReconciliationService>();
        return services;
    }
}
