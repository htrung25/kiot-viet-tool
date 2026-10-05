using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Application.DTOs;

public sealed record DiscountFeedProgramDto(
    int Id,
    string Name,
    DiscountEnum Type,
    decimal Value,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    IReadOnlyList<long> ProductIds);
