using KiotVietTool.Application.Abstractions;
using KiotVietTool.Infrastructure.Persistence;
using KiotVietTool.Infrastructure.Repositories;
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

        services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            var path = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ResolvedPath;
            options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        });

        services.AddTransient<ICustomerRepository, CustomerRepository>();
        return services;
    }

    /// <summary>Creates the DB folder and applies pending EF migrations.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var path = services.GetRequiredService<IOptions<DatabaseOptions>>().Value.ResolvedPath;
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null) Directory.CreateDirectory(directory);

        await using var db = await services.GetRequiredService<IDbContextFactory<AppDbContext>>()
            .CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
