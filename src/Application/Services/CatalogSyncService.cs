using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Exceptions;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class CatalogSyncService(
    IKiotVietConnectionRepository connections,
    ICatalogRepository catalog,
    IKiotVietApiService api,
    ISecretProtectorService secretProtector,
    TimeProvider timeProvider,
    ILogger<CatalogSyncService> logger) : ICatalogSyncService
{
    static readonly TimeSpan ModifiedFromMargin = TimeSpan.FromMinutes(5);

    readonly SemaphoreSlim _gate = new(1, 1);

    public bool IsRunning => _gate.CurrentCount == 0;

    public event EventHandler? Completed;

    public async Task<Result<SyncResultDto>> SyncAsync(IProgress<SyncProgressDto>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!await _gate.WaitAsync(0, cancellationToken))
            return Result.Failure<SyncResultDto>("Đang đồng bộ dữ liệu, hãy chờ lần đồng bộ hiện tại xong.");
        try
        {
            var connection = await connections.GetAsync(cancellationToken);
            if (connection is null) return Result.Failure<SyncResultDto>("Chưa kết nối KiotViet.");

            var secret = secretProtector.Unprotect(connection.EncryptedClientSecret);
            if (string.IsNullOrEmpty(secret))
                return Result.Failure<SyncResultDto>("Không đọc được Client Secret đã lưu (dữ liệu có thể được khôi phục từ máy khác). Hãy nhập lại Client Secret trong Kết nối KiotViet.");

            var credentials = new KiotVietCredentialsDto(connection.Retailer, connection.ClientId, secret);
            var result = await RunAsync(connection, credentials, progress, cancellationToken);
            Completed?.Invoke(this, EventArgs.Empty);
            return Result.Success(result);
        }
        catch (KiotVietApiException ex)
        {
            logger.LogWarning(ex, "Catalog sync failed");
            return Result.Failure<SyncResultDto>(ex.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Catalog sync cancelled");
            return Result.Failure<SyncResultDto>("Đã huỷ đồng bộ. Dữ liệu cũ được giữ nguyên.");
        }
        finally
        {
            _gate.Release();
        }
    }

    async Task<SyncResultDto> RunAsync(KiotVietConnection connection, KiotVietCredentialsDto credentials,
        IProgress<SyncProgressDto>? progress, CancellationToken cancellationToken)
    {
        var startedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

        progress?.Report(new SyncProgressDto("Nhóm hàng", 0, null));
        var categories = await api.GetCategoriesAsync(credentials, cancellationToken);

        var modifiedFromUtc = connection.ProductsSyncedFromUtc - ModifiedFromMargin;
        var products = new List<Product>();
        var removedIds = new HashSet<long>();
        progress?.Report(new SyncProgressDto("Sản phẩm", 0, null));
        while (true)
        {
            var page = await api.GetProductsPageAsync(credentials, modifiedFromUtc, products.Count, cancellationToken);
            products.AddRange(page.Items);
            removedIds.UnionWith(page.RemovedIds);
            progress?.Report(new SyncProgressDto("Sản phẩm", products.Count, page.Total));
            if (page.Items.Count == 0 || products.Count >= page.Total) break;
        }

        progress?.Report(new SyncProgressDto("Bảng giá", 0, null));
        var priceBooks = await api.GetPriceBooksAsync(credentials, cancellationToken);
        var toRefresh = priceBooks.Where(b => !b.IsGlobal && !b.IsExpiredAt(startedAtUtc)).ToList();
        var items = new Dictionary<long, IReadOnlyList<PriceBookItem>>();
        foreach (var priceBook in toRefresh)
        {
            items[priceBook.Id] = await api.GetPriceBookItemsAsync(credentials, priceBook.Id, cancellationToken);
            progress?.Report(new SyncProgressDto("Bảng giá", items.Count, toRefresh.Count));
        }

        progress?.Report(new SyncProgressDto("Lưu dữ liệu", 0, null));
        connection.MarkSynced(startedAtUtc);
        await catalog.ApplySyncAsync(
            new CatalogSyncDto(connection, categories, products, [.. removedIds], priceBooks, items),
            CancellationToken.None);

        logger.LogInformation(
            "Catalog synced: {Categories} categories, {Products} changed products, {Removed} removed, {PriceBooks} price books (full: {Full})",
            categories.Count, products.Count, removedIds.Count, priceBooks.Count, modifiedFromUtc is null);
        return new SyncResultDto(products.Count, removedIds.Count, priceBooks.Count, modifiedFromUtc is null);
    }
}
