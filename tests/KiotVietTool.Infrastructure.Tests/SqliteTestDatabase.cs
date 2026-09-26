using KiotVietTool.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KiotVietTool.Infrastructure.Tests;

/// <summary>In-memory SQLite kept alive by one open connection; schema built from the real migrations.</summary>
public sealed class SqliteTestDatabase : IDbContextFactory<AppDbContext>, IAsyncLifetime
{
    readonly SqliteConnection _connection = new("Data Source=:memory:");

    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
