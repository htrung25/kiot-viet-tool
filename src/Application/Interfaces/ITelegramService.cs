using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface ITelegramService
{
    Task<TelegramStatusDto?> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> VerifyBotAsync(string botToken, CancellationToken cancellationToken = default);
    Task<Result<TelegramChatDto>> DetectChatAsync(string botToken, CancellationToken cancellationToken = default);
    Task<Result> SendVerificationCodeAsync(string botToken, TelegramChatDto chat, string botUsername, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<string>>> ConfirmAsync(string code, CancellationToken cancellationToken = default);
}
