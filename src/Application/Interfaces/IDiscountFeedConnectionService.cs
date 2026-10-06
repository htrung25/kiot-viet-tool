using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface IDiscountFeedConnectionService
{
    Task<DiscountFeedConnectionDto?> GetAsync(CancellationToken cancellationToken = default);
    Task<string> SuggestScriptNameAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<CloudflareAccountDto>>> GetCloudflareAccountsAsync(string apiToken, CancellationToken cancellationToken = default);
    Task<Result<string>> DeployAsync(DeployDiscountFeedWorkerDto request, CancellationToken cancellationToken = default);
    Task<Result<string>> SaveManualAsync(SaveDiscountFeedConnectionDto request, CancellationToken cancellationToken = default);
    Task<Result<string>> DisconnectAsync(CancellationToken cancellationToken = default);
}
