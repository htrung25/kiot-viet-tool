using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KiotVietTool.Infrastructure.Repositories;

internal sealed class ProgramPriceRepository(IDbContextFactory<AppDbContext> dbFactory) : IProgramPriceRepository
{
    public async Task<IReadOnlyList<ProgramProductPrice>> GetAsync(int programId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.ProgramProductPrices.AsNoTracking().Where(p => p.ProgramId == programId).OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(DiscountProgram program, IReadOnlyCollection<ProgramProductPrice> prices, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.DiscountPrograms.Update(program);
        foreach (var price in prices)
        {
            if (price.Id == 0) db.ProgramProductPrices.Add(price);
            else db.ProgramProductPrices.Update(price);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
