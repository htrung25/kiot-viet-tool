using KiotVietTool.Application.Interfaces;
using KiotVietTool.Infrastructure.Options;
using KiotVietTool.Infrastructure.Persistence;
using KiotVietTool.Infrastructure.Repositories;
using KiotVietTool.Infrastructure.Services;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KiotVietTool.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Path), $"{DatabaseOptions.SectionName}:Path is required.")
            .ValidateOnStart();

        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.DefaultAdminUsername) && !string.IsNullOrEmpty(o.DefaultAdminPassword),
                $"{AuthOptions.SectionName}:DefaultAdminUsername and DefaultAdminPassword are required.")
            .Validate(o => o.IdleLockMinutes is >= 0 and <= 1440,
                $"{AuthOptions.SectionName}:IdleLockMinutes must be 0 (off) to 1440.")
            .Validate(o => o.IdleWarningSeconds >= 0 && (o.IdleLockMinutes == 0 || o.IdleWarningSeconds < o.IdleLockMinutes * 60),
                $"{AuthOptions.SectionName}:IdleWarningSeconds must be shorter than IdleLockMinutes.")
            .ValidateOnStart();

        services.AddOptions<KiotVietOptions>()
            .Bind(configuration.GetSection(KiotVietOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.TokenUrl, UriKind.Absolute, out _) && Uri.TryCreate(o.ApiBaseUrl, UriKind.Absolute, out _),
                $"{KiotVietOptions.SectionName}:TokenUrl and ApiBaseUrl must be absolute URLs.")
            .Validate(o => o.RequestTimeoutSeconds > 0 && o.MaxGetRequestsPerHour > 0 && o.MaxRetries >= 0,
                $"{KiotVietOptions.SectionName}: timeout, rate limit and retries must be positive.")
            .ValidateOnStart();

        services.AddOptions<DiscountFeedOptions>()
            .Bind(configuration.GetSection(DiscountFeedOptions.SectionName))
            .Validate(o => string.IsNullOrWhiteSpace(o.Url) || (Uri.TryCreate(o.Url, UriKind.Absolute, out var url)
                    && (url.Scheme == Uri.UriSchemeHttps || url.IsLoopback) && !string.IsNullOrWhiteSpace(o.WriteToken)),
                $"{DiscountFeedOptions.SectionName}: Url must be an https URL (http only for localhost) and WriteToken is required when Url is set.")
            .Validate(o => o.RequestTimeoutSeconds > 0, $"{DiscountFeedOptions.SectionName}:RequestTimeoutSeconds must be positive.")
            .ValidateOnStart();

        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.ApiBaseUrl, UriKind.Absolute, out _) && o.RequestTimeoutSeconds > 0
                && (string.IsNullOrWhiteSpace(o.ProxyUrl) || Uri.TryCreate(o.ProxyUrl, UriKind.Absolute, out _)),
                $"{TelegramOptions.SectionName}: ApiBaseUrl (and ProxyUrl when set) must be absolute URLs, timeout positive.")
            .ValidateOnStart();

        services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            var path = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ResolvedPath;
            options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        });

        services.AddSingleton<IPasswordHasherService, Pbkdf2PasswordHasherService>();
        services.AddSingleton<ISecretProtectorService, DpapiSecretProtectorService>();
        services.AddSingleton<IKiotVietApiService, KiotVietApiService>();
        services.AddTransient<IUserAccountRepository, UserAccountRepository>();
        services.AddTransient<IKiotVietConnectionRepository, KiotVietConnectionRepository>();
        services.AddTransient<ICatalogRepository, CatalogRepository>();
        services.AddTransient<IDiscountProgramRepository, DiscountProgramRepository>();
        services.AddSingleton<IDiscountFeedApiService, DiscountFeedApiService>();
        services.AddSingleton<ITelegramApiService, TelegramApiService>();
        services.AddSingleton<IOneTimeCodeService, OneTimeCodeService>();
        services.AddTransient<ITelegramConnectionRepository, TelegramConnectionRepository>();
        services.AddTransient<DatabaseInitializer>();
        return services;
    }

    /// <summary>Creates the DB folder, applies pending EF migrations and seeds the default admin if no account exists.</summary>
    public static Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken);
}
