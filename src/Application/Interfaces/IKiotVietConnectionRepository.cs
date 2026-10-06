using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.Interfaces;

public interface IKiotVietConnectionRepository
{
    Task<KiotVietConnection?> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(KiotVietConnection connection, CancellationToken cancellationToken);
    Task DeleteWithSyncedDataAsync(CancellationToken cancellationToken);
    Task UpdateInvoicesReconciledToAsync(DateTime toUtc, CancellationToken cancellationToken);
}
