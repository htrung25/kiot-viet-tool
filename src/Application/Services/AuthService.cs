using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class AuthService(
    IUserAccountRepository repository,
    IPasswordHasher passwordHasher,
    UserSession session,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    // Same message for unknown user and wrong password: don't reveal which usernames exist.
    const string InvalidCredentials = "Tên đăng nhập hoặc mật khẩu không đúng.";

    public async Task<Result<SignedInUser>> SignInAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return Result.Failure<SignedInUser>("Nhập tên đăng nhập và mật khẩu.");

        var account = await repository.GetByUsernameAsync(username.Trim(), cancellationToken);
        if (account is null || !passwordHasher.Verify(password, account.PasswordHash))
            return Result.Failure<SignedInUser>(InvalidCredentials);

        if (passwordHasher.NeedsRehash(account.PasswordHash))
            await UpgradePasswordHashAsync(account, password, cancellationToken);

        var user = new SignedInUser(account.Id, account.Username, account.MustChangePassword);
        session.Set(user);
        return Result.Success(user);
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (session.CurrentUser is not { } user) return Result.Failure("Bạn chưa đăng nhập.");

        var account = await repository.GetByIdAsync(user.Id, cancellationToken);
        if (account is null) return Result.Failure("Không tìm thấy tài khoản.");
        if (!passwordHasher.Verify(request.CurrentPassword, account.PasswordHash))
            return Result.Failure("Mật khẩu hiện tại không đúng.");
        if (request.NewPassword != request.ConfirmPassword)
            return Result.Failure("Xác nhận mật khẩu mới không khớp.");
        if (request.NewPassword == request.CurrentPassword)
            return Result.Failure("Mật khẩu mới phải khác mật khẩu hiện tại.");

        try { UserAccount.EnsureValidNewPassword(request.NewPassword); }
        catch (DomainException ex) { return Result.Failure(ex.Message); }

        account.ChangePassword(passwordHasher.Hash(request.NewPassword), timeProvider.GetUtcNow().UtcDateTime);
        await repository.UpdateAsync(account, cancellationToken);

        session.Set(user with { MustChangePassword = false });
        return Result.Success();
    }

    public void SignOut() => session.Set(null);

    // Sign-in is the only moment the plain password is available, so weak hashes are upgraded here.
    // Best effort: a failed upgrade must never block a correct sign-in; the next sign-in retries.
    async Task UpgradePasswordHashAsync(UserAccount account, string password, CancellationToken cancellationToken)
    {
        try
        {
            account.UpgradePasswordHash(passwordHasher.Hash(password), timeProvider.GetUtcNow().UtcDateTime);
            await repository.UpdateAsync(account, cancellationToken);
            logger.LogInformation("Upgraded password hash for account {AccountId}", account.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not upgrade password hash for account {AccountId}", account.Id);
        }
    }
}
