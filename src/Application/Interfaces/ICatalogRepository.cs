using KiotVietTool.Application.Common.Pagination;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.Interfaces;

public interface ICatalogRepository
{
    Task ApplySyncAsync(CatalogSyncDto sync, CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<PagedResult<ProductDto>> GetProductsAsync(ProductFilterDto filter, IReadOnlyCollection<int>? categoryIds,
        PageRequest page, CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceBookDto>> GetPriceBooksAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductDto>> GetProductsByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceBook>> GetPriceBookEntitiesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceBookItem>> GetPriceBookItemsAsync(CancellationToken cancellationToken);
}
