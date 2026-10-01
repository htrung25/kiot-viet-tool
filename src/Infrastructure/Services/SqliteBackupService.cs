using KiotVietTool.Application.Interfaces;
using KiotVietTool.Infrastructure.Options;
using KiotVietTool.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KiotVietTool.Infrastructure.Services;

internal sealed class SqliteBackupService(
    IDbContextFactory<AppDbContext> dbFactory,
    IOptions<DatabaseOptions> options,
    TimeProvider timeProvider) : IDatabaseBackupService
{
    const int KeepCount = 20;

    public async Task<string> BackupAsync(string reason, CancellationToken cancellationToken)
    {
        var databasePath = Path.GetFullPath(options.Value.ResolvedPath);
        var folder = Path.Combine(Path.GetDirectoryName(databasePath)!, "backups");
        Directory.CreateDirectory(folder);
        var safeReason = new string(reason.Where(c => char.IsAsciiLetterOrDigit(c) || c == '-').ToArray());
        var target = Path.Combine(folder, $"app-{timeProvider.GetUtcNow():yyyyMMdd-HHmmss}-{safeReason}-{Guid.NewGuid().ToString("N")[..6]}.db");

        await using (var db = await dbFactory.CreateDbContextAsync(cancellationToken))
#pragma warning disable EF1002
            await db.Database.ExecuteSqlRawAsync($"VACUUM INTO '{target.Replace("'", "''")}'", cancellationToken);
#pragma warning restore EF1002

        foreach (var old in new DirectoryInfo(folder).GetFiles("app-*.db").OrderByDescending(f => f.Name).Skip(KeepCount))
            old.Delete();
        return target;
    }
}
