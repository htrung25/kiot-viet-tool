namespace KiotVietTool.Application.DTOs;

public sealed record DiscountFeedConnectionDto(string WorkerUrl, bool IsManaged, string? CloudflareAccountId, string? ScriptName,
    string? ReadToken, DateTime ConnectedAtUtc);
