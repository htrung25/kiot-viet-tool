using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface IDiscountFeedApiService
{
    bool IsConfigured { get; }
    Task PublishAsync(DiscountFeedDto feed, CancellationToken cancellationToken);
}
