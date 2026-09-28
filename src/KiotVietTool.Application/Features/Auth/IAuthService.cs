using KiotVietTool.Application.Common;

namespace KiotVietTool.Application.Features.Auth;

public interface IAuthService
{
    Task<Result<SignedInUser>> SignInAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    void SignOut();
}
