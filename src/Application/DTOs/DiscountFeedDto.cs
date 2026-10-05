namespace KiotVietTool.Application.DTOs;

public sealed record DiscountFeedDto(long Version, DateTime GeneratedAtUtc, IReadOnlyList<DiscountFeedProgramDto> Programs);
