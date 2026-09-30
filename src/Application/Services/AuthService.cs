using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class AuthService(
    IUserAccountRepository repository,
    IPasswordHasherService passwordHasher,
    UserSessionService session,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    // Same message for unknown user and wrong password: don't reveal which usernames exist.
    const string InvalidCredentials = "Tên đăng nhập hoặc mật khẩu không đúng.";

    public async Task<Result<SignedInUserDto>> SignInAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return Result.Failure<SignedInUserDto>("Nhập tên đăng nhập và mật khẩu.");

        var account = await repository.GetByUsernameAsync(username.Trim(), cancellationToken);
        if (account is null || !await VerifyAsync(password, account.PasswordHash, cancellationToken))
            return Result.Failure<SignedInUserDto>(InvalidCredentials);

        if (passwordHasher.NeedsRehash(account.PasswordHash))
            await UpgradePasswordHashAsync(account, password, cancellationToken);

        var user = new SignedInUserDto(account.Id, account.Username, account.MustChangePassword);
        session.Set(user);
        return Result.Success(user);
    }

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

        session.Set(user with { MustChangePassword = false });
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

    public void SignOut() => session.Set(null);

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
