using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Application.DTOs;

public sealed record DiscountProgramDetailDto(
    int Id,
    string Name,
    DiscountEnum Type,
    decimal Value,
    RoundingEnum Rounding,
    StartModeEnum StartMode,
    DateTime? StartAtUtc,
    DateTime EndAtUtc,
    ScopeEnum Scope,
    int ScopeItemCount,
    string? Note,
    ProgramStatusEnum Status,
    bool IsOverdue,
    DateTime? FinishedAtUtc,
    IReadOnlyList<ProgramPriceDto> Prices)
{
    public int AppliedCount => Prices.Count(p => p.State == PriceStateEnum.Applied);
    public int FailedCount => Prices.Count(p => p.State == PriceStateEnum.Failed || (p.State == PriceStateEnum.Applied && p.LastError is not null));
    public int RestoredCount => Prices.Count(p => p.State is PriceStateEnum.Restored or PriceStateEnum.Kept);
    public int ChangedManuallyCount => Prices.Count(p => p.State == PriceStateEnum.ChangedManually);
}
