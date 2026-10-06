namespace KiotVietTool.Application.DTOs;

public sealed record TelegramStatusDto(string BotUsername, string ChatTitle, DateTime ConnectedAtUtc, int RecoveryCodesLeft,
    bool IsLoginOtpEnabled);
