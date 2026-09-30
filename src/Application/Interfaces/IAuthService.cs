using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface IAuthService
{
    Task<Result<SignedInUserDto>> SignInAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(ChangePasswordDto request, CancellationToken cancellationToken = default);
    Task<Result> VerifyCurrentUserPasswordAsync(string password, CancellationToken cancellationToken = default);
    void SignOut();
}
