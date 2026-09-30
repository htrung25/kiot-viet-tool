using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Desktop.Models;

public sealed record PreviewRowItem(DiscountPreviewRowDto Row)
{
    public string Code => Row.Code;
    public string Name => Row.FullName;
    public string Unit => Row.Unit;
    public string BasePriceText => DisplayFormat.Money(Row.BasePrice);
    public string DiscountedPriceText => Row.DiscountedPrice is { } price ? DisplayFormat.Money(price) : "—";
    public string DiscountText => Row.DiscountAmount is { } amount && Row.ActualPercent is { } percent
        ? $"−{DisplayFormat.Money(amount)} · {percent.ToString("0.##", DisplayFormat.Vietnamese)}%"
        : "";
    public bool IsExcluded => Row.Reason is not null;
    public bool HasWarning => Row.HasHighDiscount || Row.ConflictProgramName is not null;
    public string NoteText => Row.ReasonText
        ?? (Row.ConflictProgramName is { } program ? $"Trùng chương trình '{program}'" : null)
        ?? (Row.HasHighDiscount ? "Giảm từ 50% trở lên" : "");
}
