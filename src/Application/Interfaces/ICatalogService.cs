using KiotVietTool.Application.Common.Pagination;
using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ProductDto>> GetProductsAsync(ProductFilterDto filter, PageRequest page, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceBookDto>> GetPriceBooksAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> GetProductsByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
}
