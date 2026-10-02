using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KiotVietTool.Infrastructure.Repositories;

internal sealed class TelegramConnectionRepository(IDbContextFactory<AppDbContext> dbFactory) : ITelegramConnectionRepository
{
    public async Task<TelegramConnection?> GetAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.TelegramConnections.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(TelegramConnection connection, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (connection.Id == 0) db.TelegramConnections.Add(connection);
        else db.TelegramConnections.Update(connection);
        await db.SaveChangesAsync(cancellationToken);
    }
}
