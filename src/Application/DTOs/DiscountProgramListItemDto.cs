using KiotVietTool.Application.Enums;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Application.DTOs;

public sealed record DiscountProgramListItemDto(
    int Id,
    string Name,
    DiscountEnum Type,
    decimal Value,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string PriceBookName,
    ScopeEnum Scope,
    int ScopeItemCount,
    ProgramDisplayStatusEnum Status);
