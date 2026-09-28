using KiotVietTool.Application.Abstractions;
using KiotVietTool.Application.Common;
using KiotVietTool.Domain.Auth;
using KiotVietTool.Domain.Common;

namespace KiotVietTool.Application.Features.Auth;

internal sealed class AuthService(
    IUserAccountRepository repository,
    IPasswordHasher passwordHasher,
    UserSession session,
    TimeProvider timeProvider) : IAuthService
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
}
