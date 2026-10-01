namespace KiotVietTool.Application.DTOs;

public sealed record DiscountPreviewDto(
    IReadOnlyList<DiscountPreviewRowDto> Rows,
    int AppliedCount,
    int ExcludedCount,
    decimal TotalDiscount,
    int ConflictCount,
    IReadOnlyList<OverlappingPriceBookDto> OverlappingPriceBooks);
