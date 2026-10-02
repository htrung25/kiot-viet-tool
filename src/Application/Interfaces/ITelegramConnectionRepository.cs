using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.Interfaces;

public interface ITelegramConnectionRepository
{
    Task<TelegramConnection?> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(TelegramConnection connection, CancellationToken cancellationToken);
}
