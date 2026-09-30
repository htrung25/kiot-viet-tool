using System.Globalization;
using System.Text;

using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Domain.Entities;

public sealed class Product
{
    public long Id { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string FullName { get; private set; } = "";
    public int? CategoryId { get; private set; }
    public decimal BasePrice { get; private set; }
    public string Unit { get; private set; } = "";
    public long? MasterUnitId { get; private set; }
    public double? ConversionValue { get; private set; }
    public long? MasterProductId { get; private set; }
    public ProductEnum Type { get; private set; }
    public bool IsActive { get; private set; }
    public bool AllowsSale { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? ModifiedAtUtc { get; private set; }
    public string SearchKey { get; private set; } = "";

    private Product() { } // EF Core

    public static Product Create(long id, string code, string name, string? fullName, int? categoryId, decimal basePrice,
        string? unit, long? masterUnitId, double? conversionValue, long? masterProductId, ProductEnum type,
        bool isActive, bool allowsSale, DateTime? modifiedAtUtc) =>
        new()
        {
            Id = id,
            Code = code,
            Name = name,
            FullName = string.IsNullOrWhiteSpace(fullName) ? name : fullName,
            CategoryId = categoryId,
            BasePrice = basePrice,
            Unit = unit ?? "",
            MasterUnitId = masterUnitId,
            ConversionValue = conversionValue,
            MasterProductId = masterProductId,
            Type = type,
            IsActive = isActive,
            AllowsSale = allowsSale,
            ModifiedAtUtc = modifiedAtUtc,
            SearchKey = NormalizeForSearch(code + "\n" + (string.IsNullOrWhiteSpace(fullName) ? name : fullName)),
        };

    public bool IsBaseUnit => MasterUnitId is null;

    public void MarkDeleted() => IsDeleted = true;

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
