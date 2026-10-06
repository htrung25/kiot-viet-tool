using KiotVietTool.Application.DTOs;
using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.Interfaces;

public interface IKiotVietApiService
{
    Task SyncClockAsync(CancellationToken cancellationToken);
    Task<int> CountProductsAsync(KiotVietCredentialsDto credentials, CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(KiotVietCredentialsDto credentials, CancellationToken cancellationToken);
    Task<KiotVietPageDto<Product>> GetProductsPageAsync(KiotVietCredentialsDto credentials, DateTime? modifiedFromUtc,
        int currentItem, CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceBook>> GetPriceBooksAsync(KiotVietCredentialsDto credentials, CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceBookItem>> GetPriceBookItemsAsync(KiotVietCredentialsDto credentials, long priceBookId,
        CancellationToken cancellationToken);
    Task<KiotVietPageDto<KiotVietInvoiceDto>> GetInvoicesPageAsync(KiotVietCredentialsDto credentials, DateTime fromUtc, DateTime toUtc,
        int currentItem, CancellationToken cancellationToken);
}
