using KiotVietTool.Application.Features.Auth;
using KiotVietTool.Domain.Auth;
using KiotVietTool.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KiotVietTool.Infrastructure.Features.Auth;

internal sealed class UserAccountRepository(IDbContextFactory<AppDbContext> dbFactory) : IUserAccountRepository
{
    public async Task<UserAccount?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<UserAccount?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(UserAccount account, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.UserAccounts.Update(account);
        await db.SaveChangesAsync(cancellationToken);
    }
}
