using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KiotVietTool.Infrastructure.Repositories;

internal sealed class DiscountFeedConnectionRepository(IDbContextFactory<AppDbContext> dbFactory) : IDiscountFeedConnectionRepository
{
    public async Task<DiscountFeedConnection?> GetAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.DiscountFeedConnections.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(DiscountFeedConnection connection, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (connection.Id == 0) db.DiscountFeedConnections.Add(connection);
        else db.DiscountFeedConnections.Update(connection);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRevisionAsync(int id, string instanceId, long revision, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await db.DiscountFeedConnections.Where(c => c.Id == id && c.InstanceId == instanceId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.LastRevision, revision), cancellationToken);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await db.DiscountFeedConnections.ExecuteDeleteAsync(cancellationToken);
    }
}
