using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Domain.Entities;

public sealed class ProgramProductPrice
{
    public const int ErrorMaxLength = 500;

    public int Id { get; private set; }
    public int ProgramId { get; private set; }
    public long ProductId { get; private set; }
    public string ProductCode { get; private set; } = "";
    public string ProductName { get; private set; } = "";
    public decimal OriginalPrice { get; private set; }
    public decimal DiscountedPrice { get; private set; }
    public PriceStateEnum State { get; private set; }
    public DateTime? AppliedAtUtc { get; private set; }
    public DateTime? RestoredAtUtc { get; private set; }
    public string? LastError { get; private set; }

    private ProgramProductPrice() { } // EF Core

    public static ProgramProductPrice Create(int programId, long productId, string productCode, string productName,
        decimal originalPrice, decimal discountedPrice) =>
        new()
        {
            ProgramId = programId,
            ProductId = productId,
            ProductCode = productCode,
            ProductName = productName,
            OriginalPrice = originalPrice,
            DiscountedPrice = discountedPrice,
            State = PriceStateEnum.Pending,
        };

    public bool NeedsApply => State is PriceStateEnum.Pending or PriceStateEnum.Failed;

    public bool HoldsDiscount => State == PriceStateEnum.Applied;

    public void MarkApplied(DateTime nowUtc)
    {
        State = PriceStateEnum.Applied;
        AppliedAtUtc ??= nowUtc;
        LastError = null;
    }

    public void MarkApplyFailed(string error)
    {
        State = PriceStateEnum.Failed;
        LastError = Truncate(error);
    }

    public void MarkRestoreFailed(string error) => LastError = Truncate(error);

    public void MarkRestored(DateTime nowUtc)
    {
        State = PriceStateEnum.Restored;
        RestoredAtUtc = nowUtc;
        LastError = null;
    }

    public void MarkChangedManually(decimal currentPrice)
    {
        State = PriceStateEnum.ChangedManually;
        LastError = Truncate($"Giá trên KiotViet đang là {currentPrice:#,##0} ₫, khác giá tool đã ghi");
    }

    public void MarkKept(DateTime nowUtc)
    {
        State = PriceStateEnum.Kept;
        RestoredAtUtc = nowUtc;
    }

    static string Truncate(string text) => text.Length > ErrorMaxLength ? text[..ErrorMaxLength] : text;
}
