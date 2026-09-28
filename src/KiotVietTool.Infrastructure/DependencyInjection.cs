using KiotVietTool.Application.Abstractions;
using KiotVietTool.Domain.Auth;
using KiotVietTool.Infrastructure.Auth;
using KiotVietTool.Infrastructure.Persistence;
using KiotVietTool.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddTransient<ICustomerRepository, CustomerRepository>();
        services.AddTransient<IUserAccountRepository, UserAccountRepository>();
        return services;
    }

    /// <summary>Creates the DB folder, applies pending EF migrations and seeds the default admin if no account exists.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var path = services.GetRequiredService<IOptions<DatabaseOptions>>().Value.ResolvedPath;
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null) Directory.CreateDirectory(directory);

        await using var db = await services.GetRequiredService<IDbContextFactory<AppDbContext>>()
            .CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);

        if (await db.UserAccounts.AnyAsync(cancellationToken)) return;

        var auth = services.GetRequiredService<IOptions<AuthOptions>>().Value;
        var hash = services.GetRequiredService<IPasswordHasher>().Hash(auth.DefaultAdminPassword);
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        db.UserAccounts.Add(UserAccount.CreateWithTemporaryPassword(auth.DefaultAdminUsername, hash, now));
        await db.SaveChangesAsync(cancellationToken);

        services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DependencyInjection))
            .LogInformation("Seeded default admin account {Username}", auth.DefaultAdminUsername);
    }
}
