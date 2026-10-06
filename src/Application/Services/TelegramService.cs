using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Exceptions;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class TelegramService(
    ITelegramApiService telegram,
    ITelegramConnectionRepository connections,
    IUserAccountRepository accounts,
    ISecretProtectorService secretProtector,
    IOneTimeCodeService codes,
    IPasswordHasherService passwordHasher,
    UserSessionService session,
    TimeProvider timeProvider,
    ILogger<TelegramService> logger) : ITelegramService
{
    const int RecoveryCodeCount = 10;
    const int MaxAttempts = 5;
    static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    const int MaxPasswordAttempts = 5;

    PendingSetup? _pending;
    int _failedPasswordAttempts;

    public async Task<TelegramStatusDto?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var connection = await connections.GetAsync(cancellationToken);
        if (connection is null) return null;
        var recoveryCodes = session.CurrentUser is { } user && await accounts.GetByIdAsync(user.Id, cancellationToken) is { } account
            ? account.RecoveryCodeHashes.Count
            : 0;
        return new TelegramStatusDto(connection.BotUsername, connection.ChatTitle, connection.ConnectedAtUtc, recoveryCodes,
            connection.IsLoginOtpEnabled);
    }

    public async Task<Result> EnableLoginOtpAsync(CancellationToken cancellationToken = default)
    {
        if (session.CurrentUser is not { MustChangePassword: false }) return Result.Failure("Bạn chưa đăng nhập.");
        if (await connections.GetAsync(cancellationToken) is not { } connection)
            return Result.Failure("Chưa kết nối Telegram. Hãy kết nối bot trước.");
        if (connection.IsLoginOtpEnabled) return Result.Success();

        connection.SetLoginOtp(true);
        await connections.SaveAsync(connection, cancellationToken);
        logger.LogInformation("Telegram sign-in OTP enabled");
        return Result.Success();
    }

    // Turning protection off needs the password again: an unattended signed-in session must not be enough.
    public async Task<Result> DisableLoginOtpAsync(string password, CancellationToken cancellationToken = default)
    {
        if (await VerifyPasswordAsync(password, cancellationToken) is { } error) return Result.Failure(error);
        if (await connections.GetAsync(cancellationToken) is not { IsLoginOtpEnabled: true } connection) return Result.Success();

        connection.SetLoginOtp(false);
        await connections.SaveAsync(connection, cancellationToken);
        logger.LogWarning("Telegram sign-in OTP disabled; sign-in now needs the password only");
        return Result.Success();
    }

    public async Task<Result> DisconnectAsync(string password, CancellationToken cancellationToken = default)
    {
        if (await VerifyPasswordAsync(password, cancellationToken) is { } error) return Result.Failure(error);

        await connections.DeleteAsync(cancellationToken);
        if (session.CurrentUser is { } user && await accounts.GetByIdAsync(user.Id, cancellationToken) is { } account)
        {
            account.ReplaceRecoveryCodes([], timeProvider.GetUtcNow().UtcDateTime);
            await accounts.UpdateAsync(account, cancellationToken);
        }
        _pending = null;
        logger.LogWarning("Telegram disconnected; bot token and recovery codes removed");
        return Result.Success();
    }

    public async Task<Result<string>> VerifyBotAsync(string botToken, CancellationToken cancellationToken = default)
    {
        if (NormalizeToken(botToken) is not { } token) return Result.Failure<string>(InvalidTokenFormat);
        try
        {
            return Result.Success(await telegram.GetBotUsernameAsync(token, cancellationToken));
        }
        catch (TelegramApiException ex)
        {
            return Result.Failure<string>(ex.Message);
        }
    }

    public async Task<Result<TelegramChatDto>> DetectChatAsync(string botToken, CancellationToken cancellationToken = default)
    {
        if (NormalizeToken(botToken) is not { } token) return Result.Failure<TelegramChatDto>(InvalidTokenFormat);
        try
        {
            return await telegram.FindLatestStartChatAsync(token, cancellationToken) is { } chat
                ? Result.Success(chat)
                : Result.Failure<TelegramChatDto>("Chưa thấy tin /start nào gửi tới bot. Mở bot trên Telegram, bấm Bắt đầu (Start) hoặc gõ /start, rồi bấm lại nút này.");
        }
        catch (TelegramApiException ex)
        {
            return Result.Failure<TelegramChatDto>(ex.Message);
        }
    }

    public async Task<Result> SendVerificationCodeAsync(string botToken, TelegramChatDto chat, string botUsername,
        CancellationToken cancellationToken = default)
    {
        if (session.CurrentUser is not { MustChangePassword: false }) return Result.Failure("Bạn chưa đăng nhập.");
        if (NormalizeToken(botToken) is not { } token) return Result.Failure(InvalidTokenFormat);

        var code = codes.GenerateOtp();
        try
        {
            await telegram.SendMessageAsync(token, chat.ChatId,
                $"Mã xác nhận kết nối KiotViet Tool: {code}\nNhập mã này vào tool để hoàn tất. Mã hết hạn sau {CodeLifetime.TotalMinutes:0} phút.",
                cancellationToken);
        }
        catch (TelegramApiException ex)
        {
            return Result.Failure(ex.Message);
        }

        _pending = new PendingSetup(token, chat, botUsername, code, timeProvider.GetUtcNow().UtcDateTime + CodeLifetime, 0);
        logger.LogInformation("Telegram verification code sent to bot @{Bot}", botUsername);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<string>>> ConfirmAsync(string code, CancellationToken cancellationToken = default)
    {
        if (session.CurrentUser is not { MustChangePassword: false } user) return Result.Failure<IReadOnlyList<string>>("Bạn chưa đăng nhập.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (_pending is not { } pending || pending.ExpiresAtUtc <= now)
        {
            _pending = null;
            return Result.Failure<IReadOnlyList<string>>("Mã xác nhận đã hết hạn. Hãy bấm Gửi mã xác nhận lại.");
        }
        if (!codes.OtpEquals(pending.Code, (code ?? "").Trim()))
        {
            var attempts = pending.Attempts + 1;
            _pending = attempts >= MaxAttempts ? null : pending with { Attempts = attempts };
            return Result.Failure<IReadOnlyList<string>>(attempts >= MaxAttempts
                ? $"Sai mã {MaxAttempts} lần. Hãy bấm Gửi mã xác nhận để nhận mã mới."
                : $"Mã không đúng. Còn {MaxAttempts - attempts} lần thử.");
        }

        var account = await accounts.GetByIdAsync(user.Id, cancellationToken);
        if (account is null) return Result.Failure<IReadOnlyList<string>>("Không tìm thấy tài khoản.");

        var recoveryCodes = codes.GenerateRecoveryCodes(RecoveryCodeCount);
        var encryptedToken = secretProtector.Protect(pending.Token);
        try
        {
            var connection = await connections.GetAsync(cancellationToken);
            if (connection is null)
                connection = TelegramConnection.Create(encryptedToken, pending.BotUsername, pending.Chat.ChatId, pending.Chat.Title, now);
            else
                connection.Replace(encryptedToken, pending.BotUsername, pending.Chat.ChatId, pending.Chat.Title, now);
            await connections.SaveAsync(connection, cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<IReadOnlyList<string>>(ex.Message);
        }

        account.ReplaceRecoveryCodes(recoveryCodes.Select(codes.HashRecoveryCode), now);
        await accounts.UpdateAsync(account, cancellationToken);
        _pending = null;
        logger.LogInformation("Telegram connected to bot @{Bot}, chat {Chat}; {Count} recovery codes issued",
            pending.BotUsername, pending.Chat.Title, recoveryCodes.Count);
        return Result.Success(recoveryCodes);
    }

    /// <summary>
    /// Null when <paramref name="password"/> is the signed-in user's password, otherwise the error to show.
    /// Like the lock screen, <see cref="MaxPasswordAttempts"/> wrong passwords end the session.
    /// </summary>
    async Task<string?> VerifyPasswordAsync(string password, CancellationToken cancellationToken)
    {
        if (session.CurrentUser is not { MustChangePassword: false } user) return "Bạn chưa đăng nhập.";
        if (string.IsNullOrEmpty(password)) return "Nhập mật khẩu tool để xác nhận.";
        var account = await accounts.GetByIdAsync(user.Id, cancellationToken);
        if (account is null) return "Không tìm thấy tài khoản.";
        var hash = account.PasswordHash;
        if (await Task.Run(() => passwordHasher.Verify(password, hash), cancellationToken))
        {
            _failedPasswordAttempts = 0;
            return null;
        }

        if (++_failedPasswordAttempts < MaxPasswordAttempts)
            return $"Mật khẩu không đúng. Còn {MaxPasswordAttempts - _failedPasswordAttempts} lần thử trước khi bị đăng xuất.";
        _failedPasswordAttempts = 0;
        logger.LogWarning("Signed out after {Attempts} wrong passwords while changing Telegram sign-in settings", MaxPasswordAttempts);
        session.Set(null);
        return $"Sai mật khẩu {MaxPasswordAttempts} lần nên bạn đã bị đăng xuất.";
    }

    const string InvalidTokenFormat = "Bot Token không đúng định dạng. Token có dạng 123456789:AAH… (lấy từ @BotFather).";

    static string? NormalizeToken(string? botToken)
    {
        var token = (botToken ?? "").Trim();
        var colon = token.IndexOf(':');
        return colon > 0 && colon < token.Length - 1 && token[..colon].All(char.IsAsciiDigit)
            && token.All(c => char.IsAsciiLetterOrDigit(c) || c is ':' or '_' or '-')
            ? token
            : null;
    }

    sealed record PendingSetup(string Token, TelegramChatDto Chat, string BotUsername, string Code, DateTime ExpiresAtUtc, int Attempts);
}
