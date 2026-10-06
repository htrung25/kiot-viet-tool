using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

/// <summary>Optional Telegram bot that sends sign-in codes (OTP) while <see cref="TelegramStatusDto.IsLoginOtpEnabled"/> is on.</summary>
public interface ITelegramService
{
    Task<TelegramStatusDto?> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> VerifyBotAsync(string botToken, CancellationToken cancellationToken = default);
    Task<Result<TelegramChatDto>> DetectChatAsync(string botToken, CancellationToken cancellationToken = default);
    Task<Result> SendVerificationCodeAsync(string botToken, TelegramChatDto chat, string botUsername, CancellationToken cancellationToken = default);

    /// <summary>Saves the bot and chat with sign-in OTP on, and returns new recovery codes (shown once).</summary>
    Task<Result<IReadOnlyList<string>>> ConfirmAsync(string code, CancellationToken cancellationToken = default);

    Task<Result> EnableLoginOtpAsync(CancellationToken cancellationToken = default);

    /// <summary>Needs the signed-in user's password.</summary>
    Task<Result> DisableLoginOtpAsync(string password, CancellationToken cancellationToken = default);

    /// <summary>Removes the bot token and recovery codes. Needs the signed-in user's password.</summary>
    Task<Result> DisconnectAsync(string password, CancellationToken cancellationToken = default);

    // Best effort: false when Telegram is not connected or the message could not be sent.
    Task<bool> NotifyAsync(string message, CancellationToken cancellationToken = default);
}
