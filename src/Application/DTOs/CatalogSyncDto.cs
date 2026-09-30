using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.DTOs;

public sealed record CatalogSyncDto(
    KiotVietConnection Connection,
    IReadOnlyList<Category> Categories,
    IReadOnlyList<Product> ChangedProducts,
    IReadOnlyList<long> RemovedProductIds,
    IReadOnlyList<PriceBook> PriceBooks,
    IReadOnlyDictionary<long, IReadOnlyList<PriceBookItem>> PriceBookItems);
