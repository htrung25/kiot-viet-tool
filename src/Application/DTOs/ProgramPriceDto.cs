using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Application.DTOs;

public sealed record ProgramPriceDto(
    long ProductId,
    string Code,
    string Name,
    decimal OriginalPrice,
    decimal DiscountedPrice,
    PriceStateEnum State,
    string? LastError,
    DateTime? AppliedAtUtc,
    DateTime? RestoredAtUtc);
