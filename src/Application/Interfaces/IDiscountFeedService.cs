using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface IDiscountFeedService
{
    DiscountFeedStatusDto Status { get; }
    event EventHandler? Changed;
    Task<Result<string>> ActivateAsync(int programId, CancellationToken cancellationToken = default);
    Task<Result<string>> StopAsync(int programId, CancellationToken cancellationToken = default);
    Task<Result<string>> ChangeEndAsync(int programId, DateTime endAtUtc, CancellationToken cancellationToken = default);
    Task SyncAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> TakeOverAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> ChangeConnectionAsync(Func<CancellationToken, Task<Result<string>>> change, CancellationToken cancellationToken = default);
}
