using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.Interfaces;

public interface IDiscountFeedConnectionRepository
{
    Task<DiscountFeedConnection?> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(DiscountFeedConnection connection, CancellationToken cancellationToken);

    Task UpdateRevisionAsync(int id, string instanceId, long revision, CancellationToken cancellationToken);

    Task DeleteAsync(CancellationToken cancellationToken);
}
