using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KiotVietTool.Infrastructure.Repositories;

internal sealed class KiotVietConnectionRepository(IDbContextFactory<AppDbContext> dbFactory) : IKiotVietConnectionRepository
{
    public async Task<KiotVietConnection?> GetAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.KiotVietConnections.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(KiotVietConnection connection, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (connection.Id == 0) db.KiotVietConnections.Add(connection);
        else db.KiotVietConnections.Update(connection);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteWithSyncedDataAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.InvoiceReconciliations.ExecuteDeleteAsync(cancellationToken);
        await db.PriceBookItems.ExecuteDeleteAsync(cancellationToken);
        await db.PriceBooks.ExecuteDeleteAsync(cancellationToken);
        await db.Products.ExecuteDeleteAsync(cancellationToken);
        await db.Categories.ExecuteDeleteAsync(cancellationToken);
        await db.KiotVietConnections.ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateInvoicesReconciledToAsync(DateTime toUtc, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await db.KiotVietConnections.ExecuteUpdateAsync(s => s.SetProperty(c => c.InvoicesReconciledToUtc, toUtc), cancellationToken);
    }
}
