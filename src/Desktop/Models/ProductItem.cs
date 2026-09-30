using System.Globalization;

namespace KiotVietTool.Desktop.Models;

public sealed record ProductItem(
    string Code,
    string Name,
    string CategoryName,
    string Unit,
    ProductKind Kind,
    decimal BasePrice,
    bool IsActive,
    string? DiscountProgramName = null,
    decimal? DiscountedPrice = null)
{
    static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public string SearchKey { get; } = ProductFilter.NormalizeForSearch(Code + "\n" + Name);

    public string KindText => Kind switch
    {
        ProductKind.Combo => "Combo",
        ProductKind.Service => "Dịch vụ",
        _ => "Hàng hoá",
    };

    public string Details => Kind == ProductKind.Goods
        ? $"{CategoryName} · {Unit}"
        : $"{CategoryName} · {Unit} · {KindText}";

    public string BasePriceText => FormatMoney(BasePrice);
    public bool HasDiscount => DiscountProgramName is not null;
    public string? DiscountedPriceText => DiscountedPrice is { } price ? FormatMoney(price) : null;
    public string StatusText => IsActive ? "Đang kinh doanh" : "Ngừng kinh doanh";

    static string FormatMoney(decimal amount) => amount.ToString("#,##0", Vietnamese) + " ₫";
}
