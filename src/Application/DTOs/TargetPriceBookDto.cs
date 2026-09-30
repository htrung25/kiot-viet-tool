namespace KiotVietTool.Application.DTOs;

public sealed record TargetPriceBookDto(
    long Id,
    string Name,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    bool ForAllBranches,
    int BranchCount,
    bool ForAllCustomerGroups,
    int ItemCount,
    string? Problem);
