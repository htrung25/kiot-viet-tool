using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Infrastructure.Options;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KiotVietTool.Infrastructure.Persistence;

/// <summary>Runs at startup: creates the DB folder, applies pending migrations, seeds the default admin.</summary>
internal sealed class DatabaseInitializer(
    IDbContextFactory<AppDbContext> dbFactory,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<AuthOptions> authOptions,
    IPasswordHasherService passwordHasher,
    TimeProvider timeProvider,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(databaseOptions.Value.ResolvedPath));
        if (directory is not null) Directory.CreateDirectory(directory);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);

        if (await db.UserAccounts.AnyAsync(cancellationToken)) return;

        var auth = authOptions.Value;
        var hash = passwordHasher.Hash(auth.DefaultAdminPassword);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        db.UserAccounts.Add(UserAccount.CreateWithTemporaryPassword(auth.DefaultAdminUsername, hash, now));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded default admin account {Username}", auth.DefaultAdminUsername);
    }
}
