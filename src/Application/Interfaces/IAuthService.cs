using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface IAuthService
{
    Task<Result<SignInResultDto>> SignInAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<Result> VerifyOtpAsync(string code, CancellationToken cancellationToken = default);
    Task<Result<string>> SignInWithRecoveryCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Result<string>> ResendOtpAsync(CancellationToken cancellationToken = default);
    bool HasPendingSignIn { get; }
    void CancelPendingSignIn();
    Task<Result> ChangePasswordAsync(ChangePasswordDto request, CancellationToken cancellationToken = default);
    Task<Result> VerifyCurrentUserPasswordAsync(string password, CancellationToken cancellationToken = default);
    void SignOut();
}
