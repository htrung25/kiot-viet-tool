using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Enums;
using KiotVietTool.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KiotVietTool.Infrastructure.Repositories;

internal sealed class InvoiceReconciliationRepository(IDbContextFactory<AppDbContext> dbFactory) : IInvoiceReconciliationRepository
{
    static readonly ReconciliationOutcomeEnum[] Problems =
        [ReconciliationOutcomeEnum.MissingDiscount, ReconciliationOutcomeEnum.WrongAmount, ReconciliationOutcomeEnum.UnexpectedDiscount];

    public async Task<IReadOnlyDictionary<long, InvoiceReconciliation>> GetByIdsAsync(IReadOnlyCollection<long> invoiceIds,
        CancellationToken cancellationToken)
    {
        if (invoiceIds.Count == 0) return new Dictionary<long, InvoiceReconciliation>();
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.InvoiceReconciliations.AsNoTracking().Where(r => invoiceIds.Contains(r.InvoiceId))
            .ToDictionaryAsync(r => r.InvoiceId, cancellationToken);
    }

    public async Task SaveAsync(IReadOnlyList<InvoiceReconciliation> added, IReadOnlyList<InvoiceReconciliation> updated,
        CancellationToken cancellationToken)
    {
        if (added.Count == 0 && updated.Count == 0) return;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.InvoiceReconciliations.AddRange(added);
        db.InvoiceReconciliations.UpdateRange(updated);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InvoiceReconciliation>> GetPurchasedBetweenAsync(DateTime fromUtc, DateTime toUtc,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.InvoiceReconciliations.AsNoTracking()
            .Where(r => r.PurchasedAtUtc >= fromUtc && r.PurchasedAtUtc < toUtc).ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.InvoiceReconciliations.AnyAsync(cancellationToken);
    }

    public async Task<int> CountUnreviewedProblemsAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.InvoiceReconciliations.CountAsync(r => !r.IsReviewed && Problems.Contains(r.Outcome), cancellationToken);
    }

    public async Task MarkReviewedAsync(IReadOnlyCollection<long> invoiceIds, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await db.InvoiceReconciliations.Where(r => invoiceIds.Contains(r.InvoiceId))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsReviewed, true), cancellationToken);
    }
}
