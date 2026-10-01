using KiotVietTool.Application.DTOs;
using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.Interfaces;

public interface IKiotVietApiService
{
    int PriceBatchSize { get; }
    Task<IReadOnlyDictionary<long, decimal>> GetBasePricesAsync(KiotVietCredentialsDto credentials, CancellationToken cancellationToken);
    Task UpdateBasePricesAsync(KiotVietCredentialsDto credentials, IReadOnlyList<ProductPriceUpdateDto> prices, CancellationToken cancellationToken);
    Task UpdateBasePriceAsync(KiotVietCredentialsDto credentials, ProductPriceUpdateDto price, CancellationToken cancellationToken);
    Task<int> CountProductsAsync(KiotVietCredentialsDto credentials, CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(KiotVietCredentialsDto credentials, CancellationToken cancellationToken);
    Task<KiotVietPageDto<Product>> GetProductsPageAsync(KiotVietCredentialsDto credentials, DateTime? modifiedFromUtc,
        int currentItem, CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceBook>> GetPriceBooksAsync(KiotVietCredentialsDto credentials, CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceBookItem>> GetPriceBookItemsAsync(KiotVietCredentialsDto credentials, long priceBookId,
        CancellationToken cancellationToken);
}
