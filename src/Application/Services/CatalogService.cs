using KiotVietTool.Application.Common.Pagination;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.Services;

internal sealed class CatalogService(ICatalogRepository catalog, IDiscountProgramService programs, TimeProvider timeProvider)
    : ICatalogService
{
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        [.. (await catalog.GetCategoriesAsync(cancellationToken)).Select(c => new CategoryDto(c.Id, c.Name, c.ParentId))];

    public async Task<PagedResult<ProductDto>> GetProductsAsync(ProductFilterDto filter, PageRequest page,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<int>? categoryIds = null;
        if (filter.CategoryId is { } rootId)
        {
            var categories = await catalog.GetCategoriesAsync(cancellationToken);
            categoryIds = WithDescendants(rootId, categories.ToLookup(c => c.ParentId, c => c.Id));
        }
        var result = await catalog.GetProductsAsync(filter, categoryIds, page, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var live = (await programs.GetFeedProgramsAsync(cancellationToken))
            .Where(p => p.StartAtUtc <= now)
            .SelectMany(p => p.ProductIds.Select(id => (id, p)))
            .DistinctBy(x => x.id)
            .ToDictionary(x => x.id, x => x.p);
        return result.Map(product => live.TryGetValue(product.Id, out var p)
            ? product with
            {
                DiscountProgramName = p.Name,
                DiscountedPrice = DiscountProgram.CalculateDiscountedPrice(product.BasePrice, p.Type, p.Value),
            }
            : product);
    }

    public Task<IReadOnlyList<PriceBookDto>> GetPriceBooksAsync(CancellationToken cancellationToken = default) =>
        catalog.GetPriceBooksAsync(cancellationToken);

    public Task<IReadOnlyList<ProductDto>> GetProductsByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default) =>
        ids.Count == 0 ? Task.FromResult<IReadOnlyList<ProductDto>>([]) : catalog.GetProductsByIdsAsync(ids, cancellationToken);

    static HashSet<int> WithDescendants(int rootId, ILookup<int?, int> childrenByParent)
    {
        var result = new HashSet<int>();
        var pending = new Stack<int>([rootId]);
        while (pending.TryPop(out var id))
            if (result.Add(id))
                foreach (var child in childrenByParent[id]) pending.Push(child);
        return result;
    }
}
