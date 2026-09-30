using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface IKiotVietConnectionService
{
    Task<KiotVietConnectionDto?> GetAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> TestAsync(SaveKiotVietConnectionDto request, CancellationToken cancellationToken = default);
    Task<Result> SaveAsync(SaveKiotVietConnectionDto request, CancellationToken cancellationToken = default);
    Task<Result> DisconnectAsync(CancellationToken cancellationToken = default);
}
