using KiotVietTool.Domain.Auth;

namespace KiotVietTool.Application.Features.Auth;

public interface IUserAccountRepository
{
    /// <summary>Case-insensitive match.</summary>
    Task<UserAccount?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<UserAccount?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task UpdateAsync(UserAccount account, CancellationToken cancellationToken);
}
