using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Application.DTOs;

public sealed record DiscountProgramDetailDto(
    int Id,
    string Name,
    DiscountEnum Type,
    decimal Value,
    StartModeEnum StartMode,
    DateTime? StartAtUtc,
    DateTime EndAtUtc,
    ScopeEnum Scope,
    int ScopeItemCount,
    string? Note,
    ProgramPhaseEnum Phase,
    DateTime? FinishedAtUtc,
    DiscountPreviewDto Preview);
