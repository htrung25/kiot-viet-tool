using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.Interfaces;

public interface IInvoiceReconciliationRepository
{
    Task<IReadOnlyDictionary<long, InvoiceReconciliation>> GetByIdsAsync(IReadOnlyCollection<long> invoiceIds, CancellationToken cancellationToken);
    Task SaveAsync(IReadOnlyList<InvoiceReconciliation> added, IReadOnlyList<InvoiceReconciliation> updated, CancellationToken cancellationToken);
    Task<IReadOnlyList<InvoiceReconciliation>> GetPurchasedBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
    Task<bool> AnyAsync(CancellationToken cancellationToken);
    Task<int> CountUnreviewedProblemsAsync(CancellationToken cancellationToken);
    Task MarkReviewedAsync(IReadOnlyCollection<long> invoiceIds, CancellationToken cancellationToken);
}
