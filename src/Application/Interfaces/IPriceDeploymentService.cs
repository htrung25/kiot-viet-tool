using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface IPriceDeploymentService
{
    bool IsRunning { get; }
    event EventHandler? Changed;
    Task<Result<string>> ActivateAsync(int programId, IProgress<DeploymentProgressDto>? progress = null, CancellationToken cancellationToken = default);
    Task<Result<string>> StopAsync(int programId, IProgress<DeploymentProgressDto>? progress = null, CancellationToken cancellationToken = default);
    Task<Result<string>> RetryAsync(int programId, IProgress<DeploymentProgressDto>? progress = null, CancellationToken cancellationToken = default);
    Task<Result<string>> ChangeEndAsync(int programId, DateTime endAtUtc, CancellationToken cancellationToken = default);
    Task<Result<string>> ResolveManualChangesAsync(int programId, bool restoreOriginal, CancellationToken cancellationToken = default);
    Task<int> RunDueAsync(CancellationToken cancellationToken = default);
}
