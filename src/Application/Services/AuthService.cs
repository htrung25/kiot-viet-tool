using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Exceptions;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class AuthService(
    IUserAccountRepository repository,
    IPasswordHasherService passwordHasher,
    ITelegramConnectionRepository telegramConnections,
    ITelegramApiService telegram,
    ISecretProtectorService secretProtector,
    IOneTimeCodeService codes,
    PendingSignInService pending,
    UserSessionService session,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    // Same message for unknown user and wrong password: don't reveal which usernames exist.
    const string InvalidCredentials = "Tên đăng nhập hoặc mật khẩu không đúng.";
    const int MaxOtpAttempts = 5;
    static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
    static readonly TimeSpan ResendDelay = TimeSpan.FromSeconds(60);

    public async Task<Result<SignInResultDto>> SignInAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        pending.Current = null;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return Result.Failure<SignInResultDto>("Nhập tên đăng nhập và mật khẩu.");

        var account = await repository.GetByUsernameAsync(username.Trim(), cancellationToken);
        if (account is null || !await VerifyAsync(password, account.PasswordHash, cancellationToken))
            return Result.Failure<SignInResultDto>(InvalidCredentials);
        if (LockedMessage(account) is { } locked) return Result.Failure<SignInResultDto>(locked);

        if (passwordHasher.NeedsRehash(account.PasswordHash))
            await UpgradePasswordHashAsync(account, password, cancellationToken);

        var connection = await telegramConnections.GetAsync(cancellationToken);
        if (connection is null)
        {
            session.Set(new SignedInUserDto(account.Id, account.Username, account.MustChangePassword, NeedsTelegram: true));
            return Result.Success(new SignInResultDto(OtpRequired: false, null));
        }

        var notice = await SendOtpAsync(account.Id, connection, cancellationToken);
        return Result.Success(new SignInResultDto(OtpRequired: true, notice));
    }

    public async Task<Result> VerifyOtpAsync(string code, CancellationToken cancellationToken = default)
    {
        if (ActiveChallenge() is not { } challenge) return Result.Failure("Mã đã hết hạn. Hãy đăng nhập lại để nhận mã mới.");
        var account = await repository.GetByIdAsync(challenge.AccountId, cancellationToken);
        if (account is null) return Result.Failure("Không tìm thấy tài khoản.");
        if (LockedMessage(account) is { } locked) return Result.Failure(locked);

        if (codes.OtpEquals(challenge.Code, (code ?? "").Trim()))
        {
            await CompleteSignInAsync(account, cancellationToken);
            logger.LogInformation("Account {AccountId} signed in with a Telegram OTP", account.Id);
            return Result.Success();
        }

        account.RegisterOtpFailure(UtcNow);
        await repository.UpdateAsync(account, cancellationToken);
        var attempts = challenge.Attempts + 1;
        logger.LogWarning("Wrong OTP for account {AccountId} (attempt {Attempt})", account.Id, attempts);
        if (LockedMessage(account) is { } nowLocked)
        {
            pending.Current = null;
            return Result.Failure(nowLocked);
        }
        if (attempts >= MaxOtpAttempts)
        {
            pending.Current = null;
            return Result.Failure($"Sai mã {MaxOtpAttempts} lần. Hãy đăng nhập lại để nhận mã mới.");
        }
        pending.Current = challenge with { Attempts = attempts };
        return Result.Failure($"Mã không đúng. Còn {MaxOtpAttempts - attempts} lần thử.");
    }

    public async Task<Result<string>> SignInWithRecoveryCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (ActiveChallenge() is not { } challenge) return Result.Failure<string>("Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại.");
        var account = await repository.GetByIdAsync(challenge.AccountId, cancellationToken);
        if (account is null) return Result.Failure<string>("Không tìm thấy tài khoản.");
        if (LockedMessage(account) is { } locked) return Result.Failure<string>(locked);

        var entered = (code ?? "").Trim();
        if (entered.Length == 0 || !account.ConsumeRecoveryCode(hash => codes.VerifyRecoveryCode(entered, hash), UtcNow))
        {
            account.RegisterOtpFailure(UtcNow);
            await repository.UpdateAsync(account, cancellationToken);
            logger.LogWarning("Wrong recovery code for account {AccountId}", account.Id);
            if (LockedMessage(account) is { } nowLocked)
            {
                pending.Current = null;
                return Result.Failure<string>(nowLocked);
            }
            return Result.Failure<string>("Mã dự phòng không đúng hoặc đã được dùng.");
        }

        await CompleteSignInAsync(account, cancellationToken);
        logger.LogWarning("Account {AccountId} signed in with a recovery code; {Left} left", account.Id, account.RecoveryCodeHashes.Count);
        return Result.Success($"Đã dùng 1 mã dự phòng, còn {account.RecoveryCodeHashes.Count} mã. Nếu Telegram có vấn đề, hãy kết nối lại Telegram để nhận bộ mã mới.");
    }

    public async Task<Result<string>> ResendOtpAsync(CancellationToken cancellationToken = default)
    {
        if (ActiveChallenge() is not { } challenge) return Result.Failure<string>("Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại.");
        var wait = challenge.SentAtUtc + ResendDelay - UtcNow;
        if (wait > TimeSpan.Zero) return Result.Failure<string>($"Chờ {Math.Ceiling(wait.TotalSeconds)} giây rồi gửi lại mã.");
        var connection = await telegramConnections.GetAsync(cancellationToken);
        if (connection is null) return Result.Failure<string>("Chưa kết nối Telegram.");
        return Result.Success(await SendOtpAsync(challenge.AccountId, connection, cancellationToken));
    }

    public bool HasPendingSignIn => ActiveChallenge() is not null;

    public void CancelPendingSignIn() => pending.Current = null;

    public async Task<Result> ChangePasswordAsync(ChangePasswordDto request, CancellationToken cancellationToken = default)
    {
        if (session.CurrentUser is not { } user) return Result.Failure("Bạn chưa đăng nhập.");

        var account = await repository.GetByIdAsync(user.Id, cancellationToken);
        if (account is null) return Result.Failure("Không tìm thấy tài khoản.");
        if (request.NewPassword != request.ConfirmPassword)
            return Result.Failure("Xác nhận mật khẩu mới không khớp.");
        if (request.NewPassword == request.CurrentPassword)
            return Result.Failure("Mật khẩu mới phải khác mật khẩu hiện tại.");

        try { UserAccount.EnsureValidNewPassword(request.NewPassword); }
        catch (DomainException ex) { return Result.Failure(ex.Message); }

        if (!await VerifyAsync(request.CurrentPassword, account.PasswordHash, cancellationToken))
            return Result.Failure("Mật khẩu hiện tại không đúng.");

        var newHash = await HashAsync(request.NewPassword, cancellationToken);
        account.ChangePassword(newHash, timeProvider.GetUtcNow().UtcDateTime);
        await repository.UpdateAsync(account, cancellationToken);

        session.Set(user with { MustChangePassword = false, NeedsTelegram = await telegramConnections.GetAsync(cancellationToken) is null });
        return Result.Success();
    }

    public async Task<Result> VerifyCurrentUserPasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        if (session.CurrentUser is not { } user) return Result.Failure("Bạn chưa đăng nhập.");
        if (string.IsNullOrEmpty(password)) return Result.Failure("Nhập mật khẩu để mở khoá.");

        var account = await repository.GetByIdAsync(user.Id, cancellationToken);
        if (account is null || !await VerifyAsync(password, account.PasswordHash, cancellationToken))
            return Result.Failure("Mật khẩu không đúng.");
        return Result.Success();
    }

    public void SignOut()
    {
        pending.Current = null;
        session.Set(null);
    }

    DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    PendingSignInService.Challenge? ActiveChallenge()
    {
        if (pending.Current is { } challenge && challenge.ExpiresAtUtc > UtcNow) return challenge;
        pending.Current = null;
        return null;
    }

    string? LockedMessage(UserAccount account) => account.IsOtpLockedAt(UtcNow)
        ? $"Đăng nhập tạm khoá do nhập sai mã nhiều lần. Thử lại sau {Math.Ceiling((account.OtpLockedUntilUtc!.Value - UtcNow).TotalMinutes)} phút."
        : null;

    async Task<string> SendOtpAsync(int accountId, TelegramConnection connection, CancellationToken cancellationToken)
    {
        var code = codes.GenerateOtp();
        var now = UtcNow;
        var challenge = new PendingSignInService.Challenge(accountId, code, now + OtpLifetime, now, 0);
        pending.Current = challenge;

        var token = secretProtector.Unprotect(connection.EncryptedBotToken);
        if (string.IsNullOrEmpty(token))
            return "Không đọc được Bot Token đã lưu nên chưa gửi được mã. Hãy dùng mã dự phòng rồi kết nối lại Telegram.";
        try
        {
            await telegram.SendMessageAsync(token, connection.ChatId,
                $"Mã đăng nhập KiotViet Tool: {code}\nMã hết hạn sau {OtpLifetime.TotalMinutes:0} phút. Nếu không phải bạn đang đăng nhập, hãy đổi mật khẩu tool ngay.",
                cancellationToken);
            logger.LogInformation("Login OTP sent to Telegram for account {AccountId}", accountId);
            return $"Đã gửi mã 6 số tới Telegram ({connection.ChatTitle}). Mã hết hạn sau {OtpLifetime.TotalMinutes:0} phút.";
        }
        catch (TelegramApiException ex)
        {
            logger.LogWarning(ex, "Could not send login OTP to Telegram for account {AccountId}", accountId);
            pending.Current = challenge with { SentAtUtc = DateTime.MinValue };
            return $"Không gửi được mã qua Telegram: {ex.Message} Bạn có thể bấm Gửi lại mã hoặc dùng mã dự phòng.";
        }
    }

    async Task CompleteSignInAsync(UserAccount account, CancellationToken cancellationToken)
    {
        pending.Current = null;
        account.ResetOtpFailures();
        await repository.UpdateAsync(account, cancellationToken);
        session.Set(new SignedInUserDto(account.Id, account.Username, account.MustChangePassword, NeedsTelegram: false));
    }

    // Sign-in is the only moment the plain password is available, so weak hashes are upgraded here.
    // Best effort: a failed upgrade must never block a correct sign-in; the next sign-in retries.
    async Task UpgradePasswordHashAsync(UserAccount account, string password, CancellationToken cancellationToken)
    {
        try
        {
            account.UpgradePasswordHash(await HashAsync(password, cancellationToken), timeProvider.GetUtcNow().UtcDateTime);
            await repository.UpdateAsync(account, cancellationToken);
            logger.LogInformation("Upgraded password hash for account {AccountId}", account.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not upgrade password hash for account {AccountId}", account.Id);
        }
    }

    Task<bool> VerifyAsync(string password, string hash, CancellationToken cancellationToken) =>
        Task.Run(() => passwordHasher.Verify(password, hash), cancellationToken);

    Task<string> HashAsync(string password, CancellationToken cancellationToken) =>
        Task.Run(() => passwordHasher.Hash(password), cancellationToken);
}
