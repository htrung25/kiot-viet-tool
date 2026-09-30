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
            .ValidateOnStart();

        services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            var path = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ResolvedPath;
            options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        });

        services.AddSingleton<IPasswordHasherService, Pbkdf2PasswordHasherService>();
        services.AddTransient<IUserAccountRepository, UserAccountRepository>();
        services.AddTransient<DatabaseInitializer>();
        return services;
    }

    /// <summary>Creates the DB folder, applies pending EF migrations and seeds the default admin if no account exists.</summary>
    public static Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken);
}
