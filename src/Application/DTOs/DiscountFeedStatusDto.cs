namespace KiotVietTool.Application.DTOs;

public sealed record DiscountFeedStatusDto(bool IsConfigured, DateTime? LastPublishedAtUtc, string? LastError);
