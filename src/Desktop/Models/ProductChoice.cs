using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Desktop.Models;

public sealed record ProductChoice(long Id, string Code, string Name, string Details)
{
    public static ProductChoice From(ProductDto product) =>
        new(product.Id, product.Code, product.FullName,
            string.Join(" · ", new[] { product.CategoryName, product.Unit, DisplayFormat.Money(product.BasePrice) }
                .Where(s => !string.IsNullOrEmpty(s))));
}
