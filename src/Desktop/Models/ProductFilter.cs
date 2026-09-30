using System.Globalization;
using System.Text;

namespace KiotVietTool.Desktop.Models;

public sealed record ProductFilter(string? Keyword, string? Category, ProductKind? Kind, bool? IsActive)
{
    readonly string _normalizedKeyword = NormalizeForSearch(Keyword);

    public bool IsEmpty => _normalizedKeyword.Length == 0 && Category is null && Kind is null && IsActive is null;

    public bool Matches(ProductItem product) =>
        (_normalizedKeyword.Length == 0 || product.SearchKey.Contains(_normalizedKeyword, StringComparison.Ordinal))
        && (Category is null || product.CategoryName == Category)
        && (Kind is null || product.Kind == Kind)
        && (IsActive is null || product.IsActive == IsActive);

    public static string NormalizeForSearch(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var decomposed = text.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c);
        return builder.ToString();
    }
}
