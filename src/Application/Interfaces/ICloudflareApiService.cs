using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface ICloudflareApiService
{
    Task<IReadOnlyList<CloudflareAccountDto>> GetAccountsAsync(string apiToken, CancellationToken cancellationToken);

    Task<string> DeployDiscountFeedWorkerAsync(CloudflareWorkerDeployDto request, CancellationToken cancellationToken);
}
