using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Application.DTOs;

public sealed record DiscountProgramListItemDto(
    int Id,
    string Name,
    DiscountEnum Type,
    decimal Value,
    StartModeEnum StartMode,
    DateTime? StartAtUtc,
    DateTime EndAtUtc,
    ScopeEnum Scope,
    int ScopeItemCount,
    ProgramStatusEnum Status,
    bool IsOverdue);
