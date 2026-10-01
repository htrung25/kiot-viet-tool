using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Application.DTOs;

public sealed record SaveDiscountProgramDto(
    int? Id,
    string Name,
    DiscountEnum Type,
    decimal Value,
    RoundingEnum Rounding,
    StartModeEnum StartMode,
    DateTime? StartAtUtc,
    DateTime EndAtUtc,
    ScopeEnum Scope,
    IReadOnlyList<int> CategoryIds,
    IReadOnlyList<long> ProductIds,
    IReadOnlyList<long> ExcludedProductIds,
    UnitScopeEnum UnitScope,
    string? Note);
