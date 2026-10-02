using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface ITelegramApiService
{
    Task<string> GetBotUsernameAsync(string botToken, CancellationToken cancellationToken);
    Task<TelegramChatDto?> FindLatestStartChatAsync(string botToken, CancellationToken cancellationToken);
    Task SendMessageAsync(string botToken, long chatId, string text, CancellationToken cancellationToken);
}
