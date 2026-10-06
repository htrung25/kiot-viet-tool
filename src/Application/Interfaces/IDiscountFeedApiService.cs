using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface IDiscountFeedApiService
{
    Task<DiscountFeedRemoteStateDto> GetStateAsync(DiscountFeedEndpointDto endpoint, CancellationToken cancellationToken);

    Task<long> PublishAsync(DiscountFeedEndpointDto endpoint, DiscountFeedDto feed, long expectedRevision, CancellationToken cancellationToken);
}
