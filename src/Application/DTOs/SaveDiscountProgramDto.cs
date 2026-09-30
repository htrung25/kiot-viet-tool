using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Application.DTOs;

public sealed record SaveDiscountProgramDto(
    int? Id,
    string Name,
    DiscountEnum Type,
    decimal Value,
    RoundingEnum Rounding,
    long? TargetPriceBookId,
    ScopeEnum Scope,
    IReadOnlyList<int> CategoryIds,
    IReadOnlyList<long> ProductIds,
    IReadOnlyList<long> ExcludedProductIds,
    UnitScopeEnum UnitScope,
    string? Note);
