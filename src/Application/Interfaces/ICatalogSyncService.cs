using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface ICatalogSyncService
{
    bool IsRunning { get; }
    event EventHandler? Completed;
    Task<Result<SyncResultDto>> SyncAsync(IProgress<SyncProgressDto>? progress = null, CancellationToken cancellationToken = default);
}
