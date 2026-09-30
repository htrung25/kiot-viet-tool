namespace KiotVietTool.Domain.Entities;

public sealed class PriceBookItem
{
    public long PriceBookId { get; private set; }
    public long ProductId { get; private set; }
    public decimal Price { get; private set; }

    private PriceBookItem() { } // EF Core

    public static PriceBookItem Create(long priceBookId, long productId, decimal price) =>
        new() { PriceBookId = priceBookId, ProductId = productId, Price = price };
}
