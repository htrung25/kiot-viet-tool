using KiotVietTool.Application.Common.Pagination;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Enums;
using KiotVietTool.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KiotVietTool.Infrastructure.Repositories;

internal sealed class CatalogRepository(IDbContextFactory<AppDbContext> dbFactory) : ICatalogRepository
{
    public async Task ApplySyncAsync(CatalogSyncDto sync, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.Categories.ExecuteDeleteAsync(cancellationToken);
        db.Categories.AddRange(sync.Categories.DistinctBy(c => c.Id));

        var changedIds = sync.ChangedProducts.Select(p => p.Id).ToList();
        var existingProductIds = (await db.Products.Where(p => changedIds.Contains(p.Id)).Select(p => p.Id)
            .ToListAsync(cancellationToken)).ToHashSet();
        foreach (var product in sync.ChangedProducts.DistinctBy(p => p.Id))
        {
            if (existingProductIds.Contains(product.Id)) db.Products.Update(product);
            else db.Products.Add(product);
        }

        var removedIds = sync.RemovedProductIds.Except(changedIds).ToList();
        await db.Products.Where(p => removedIds.Contains(p.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true), cancellationToken);

        var priceBookIds = sync.PriceBooks.Select(b => b.Id).ToList();
        var existingPriceBookIds = (await db.PriceBooks.Select(b => b.Id).ToListAsync(cancellationToken)).ToHashSet();
        await db.PriceBooks.Where(b => !priceBookIds.Contains(b.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, true), cancellationToken);
        await db.PriceBookItems.Where(i => !priceBookIds.Contains(i.PriceBookId)).ExecuteDeleteAsync(cancellationToken);
        foreach (var priceBook in sync.PriceBooks.DistinctBy(b => b.Id))
        {
            if (existingPriceBookIds.Contains(priceBook.Id)) db.PriceBooks.Update(priceBook);
            else db.PriceBooks.Add(priceBook);
        }

        foreach (var (priceBookId, items) in sync.PriceBookItems)
        {
            await db.PriceBookItems.Where(i => i.PriceBookId == priceBookId).ExecuteDeleteAsync(cancellationToken);
            db.PriceBookItems.AddRange(items.DistinctBy(i => i.ProductId));
        }

        db.KiotVietConnections.Update(sync.Connection);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Categories.AsNoTracking().OrderBy(c => c.Name).ThenBy(c => c.Id).ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(ProductFilterDto filter, IReadOnlyCollection<int>? categoryIds,
        PageRequest page, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Products.AsNoTracking().Where(p => !p.IsDeleted);

        var keyword = Product.NormalizeForSearch(filter.Keyword);
        if (keyword.Length > 0) query = query.Where(p => p.SearchKey.Contains(keyword));
        if (categoryIds is not null) query = query.Where(p => p.CategoryId != null && categoryIds.Contains(p.CategoryId.Value));
        if (filter.Type is { } type) query = query.Where(p => p.Type == type);
        if (filter.IsActive is { } isActive) query = query.Where(p => p.IsActive == isActive);

        var total = page.IncludeTotal ? await query.CountAsync(cancellationToken) : 0;
        var rows = await ToDtos(db, query.OrderBy(p => p.Code).ThenBy(p => p.Id).Skip(page.Skip)
                .Take(page.IncludeTotal ? page.PageSize : page.PageSize + 1))
            .ToListAsync(cancellationToken);

        return page.IncludeTotal ? PagedResult<ProductDto>.Create(rows, page, total) : PagedResult<ProductDto>.CreateWithoutTotal(rows, page);
    }

    public async Task<IReadOnlyList<PriceBookDto>> GetPriceBooksAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var books = await db.PriceBooks.AsNoTracking().Where(b => !b.IsDeleted).ToListAsync(cancellationToken);
        var counts = await db.PriceBookItems.GroupBy(i => i.PriceBookId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        return [.. books
            .OrderByDescending(b => b.StartAtUtc).ThenBy(b => b.Id)
            .Select(b => new PriceBookDto(b.Id, b.Name, b.IsActive, b.IsGlobal, b.StartAtUtc, b.EndAtUtc, b.ForAllBranches,
                counts.GetValueOrDefault(b.Id)))];
    }

    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Products.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await ToDtos(db, db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).OrderBy(p => p.Code).ThenBy(p => p.Id))
            .ToListAsync(cancellationToken);
    }

    static readonly ProgramStatusEnum[] HoldingStatuses =
    [
        ProgramStatusEnum.Applying, ProgramStatusEnum.Running, ProgramStatusEnum.ApplyFailed,
        ProgramStatusEnum.Restoring, ProgramStatusEnum.RestoreFailed,
    ];

    static IQueryable<ProductDto> ToDtos(AppDbContext db, IQueryable<Product> products)
    {
        var discounts =
            from pp in db.ProgramProductPrices
            join dp in db.DiscountPrograms on pp.ProgramId equals dp.Id
            where pp.State == PriceStateEnum.Applied && HoldingStatuses.Contains(dp.Status)
            select new { pp.ProductId, dp.Name, pp.OriginalPrice, pp.DiscountedPrice };

        return
            from p in products
            join c in db.Categories on p.CategoryId equals c.Id into categories
            from c in categories.DefaultIfEmpty()
            join d in discounts on p.Id equals d.ProductId into active
            from d in active.DefaultIfEmpty()
            select new ProductDto(p.Id, p.Code, p.FullName, c == null ? null : c.Name, p.Unit, p.Type, p.BasePrice, p.IsActive,
                p.AllowsSale, d == null ? null : d.Name, d == null ? null : d.OriginalPrice, d == null ? null : d.DiscountedPrice);
    }

    public async Task<IReadOnlyList<PriceBook>> GetPriceBookEntitiesAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.PriceBooks.AsNoTracking().Where(b => !b.IsDeleted).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceBookItem>> GetPriceBookItemsAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.PriceBookItems.AsNoTracking().ToListAsync(cancellationToken);
    }
}
