using KiotVietTool.Application.DTOs;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Desktop.Models;

public sealed record ProductItem(
    string Code,
    string Name,
    string? CategoryName,
    string Unit,
    ProductEnum Type,
    decimal BasePrice,
    bool IsActive,
    string? DiscountProgramName = null,
    decimal? DiscountedPrice = null)
{
    public static ProductItem From(ProductDto product) =>
        new(product.Code, product.FullName, product.CategoryName, product.Unit, product.Type, product.BasePrice, product.IsActive);

    public string KindText => Type switch
    {
        ProductEnum.Combo => "Combo",
        ProductEnum.Service => "Dịch vụ",
        _ => "Hàng hoá",
    };

    public string Details => string.Join(" · ",
        new[] { CategoryName, Unit, Type == ProductEnum.Goods ? null : KindText }.Where(s => !string.IsNullOrEmpty(s)));

    public string BasePriceText => DisplayFormat.Money(BasePrice);
    public bool HasDiscount => DiscountProgramName is not null;
    public string? DiscountedPriceText => DiscountedPrice is { } price ? DisplayFormat.Money(price) : null;
    public string StatusText => IsActive ? "Đang kinh doanh" : "Ngừng kinh doanh";
}
