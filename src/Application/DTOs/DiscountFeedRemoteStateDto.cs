namespace KiotVietTool.Application.DTOs;

public sealed record DiscountFeedRemoteStateDto(long Revision, string? InstanceId, string? ContentHash, DateTime? GeneratedAtUtc,
    string? TokenRole = null);
