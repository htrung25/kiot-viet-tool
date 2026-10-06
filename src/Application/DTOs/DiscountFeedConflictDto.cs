namespace KiotVietTool.Application.DTOs;

public sealed record DiscountFeedConflictDto(long Revision, bool ByOtherInstallation, DateTime? WrittenAtUtc);
