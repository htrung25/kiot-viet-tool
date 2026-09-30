using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Application.DTOs;

public sealed record DiscountPreviewRowDto(
    long ProductId,
    string Code,
    string FullName,
    string Unit,
    decimal BasePrice,
    decimal? DiscountedPrice,
    ExclusionReasonEnum? Reason,
    string? ReasonText,
    bool HasHighDiscount,
    string? ConflictProgramName)
{
    public decimal? DiscountAmount => DiscountedPrice is { } price ? BasePrice - price : null;
    public decimal? ActualPercent => DiscountAmount is { } amount && BasePrice > 0 ? Math.Round(amount * 100 / BasePrice, 2) : null;
}
