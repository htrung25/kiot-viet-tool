namespace KiotVietTool.Application.DTOs;

public sealed record DiscountFeedDto(long Version, DateTime GeneratedAtUtc, string InstanceId, string ContentHash,
    IReadOnlyList<DiscountFeedProgramDto> Programs);
