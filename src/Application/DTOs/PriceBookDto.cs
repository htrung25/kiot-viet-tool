namespace KiotVietTool.Application.DTOs;

public sealed record PriceBookDto(
    long Id,
    string Name,
    bool IsActive,
    bool IsGlobal,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    bool ForAllBranches,
    int ProductCount);
