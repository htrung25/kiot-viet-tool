using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KiotVietTool.Infrastructure.Repositories;

internal sealed class DiscountProgramRepository(IDbContextFactory<AppDbContext> dbFactory) : IDiscountProgramRepository
{
    public async Task<IReadOnlyList<DiscountProgram>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.DiscountPrograms.AsNoTracking().OrderBy(p => p.Id).ToListAsync(cancellationToken);
    }

    public async Task<DiscountProgram?> GetAsync(int id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.DiscountPrograms.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AddAsync(DiscountProgram program, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.DiscountPrograms.Add(program);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(DiscountProgram program, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.DiscountPrograms.Update(program);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(DiscountProgram program, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.DiscountPrograms.Remove(program);
        await db.SaveChangesAsync(cancellationToken);
    }
}
