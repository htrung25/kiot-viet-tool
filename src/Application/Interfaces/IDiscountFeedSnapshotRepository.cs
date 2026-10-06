using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.Interfaces;

public interface IDiscountFeedSnapshotRepository
{
    Task AddAsync(DiscountFeedSnapshot snapshot, CancellationToken cancellationToken);
    Task<DateTime?> GetFirstPublishedAtAsync(CancellationToken cancellationToken);

    // The snapshot in effect at fromUtc plus every later one, oldest first.
    Task<IReadOnlyList<DiscountFeedSnapshot>> GetFromAsync(DateTime fromUtc, CancellationToken cancellationToken);
}
