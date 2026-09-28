using KiotVietTool.Application.Features.Customers;
using KiotVietTool.Domain.Customers;
using KiotVietTool.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KiotVietTool.Infrastructure.Features.Customers;

internal sealed class CustomerRepository(IDbContextFactory<AppDbContext> dbFactory) : ICustomerRepository
{
    public async Task<IReadOnlyList<Customer>> SearchAsync(string? keyword, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Code, pattern) ||
                EF.Functions.Like(c.Name, pattern) ||
                (c.Phone != null && EF.Functions.Like(c.Phone, pattern)));
        }
        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Customers.AnyAsync(c => c.Code == code && c.Id != excludeId, cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.Customers.Update(customer);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Customers.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
    }
}
