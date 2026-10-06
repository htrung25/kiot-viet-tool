using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KiotVietTool.Infrastructure.Repositories;

internal sealed class DiscountFeedSnapshotRepository(IDbContextFactory<AppDbContext> dbFactory) : IDiscountFeedSnapshotRepository
{
    public async Task AddAsync(DiscountFeedSnapshot snapshot, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.DiscountFeedSnapshots.Add(snapshot);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DateTime?> GetFirstPublishedAtAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.DiscountFeedSnapshots.OrderBy(s => s.PublishedAtUtc).Select(s => (DateTime?)s.PublishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DiscountFeedSnapshot>> GetFromAsync(DateTime fromUtc, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var inEffect = await db.DiscountFeedSnapshots.AsNoTracking().Where(s => s.PublishedAtUtc <= fromUtc)
            .OrderByDescending(s => s.PublishedAtUtc).ThenByDescending(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        var later = await db.DiscountFeedSnapshots.AsNoTracking().Where(s => s.PublishedAtUtc > fromUtc)
            .OrderBy(s => s.PublishedAtUtc).ThenBy(s => s.Id).ToListAsync(cancellationToken);
        return inEffect is null ? later : [inEffect, .. later];
    }
}
