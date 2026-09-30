namespace KiotVietTool.Application.DTOs;

public sealed record SyncProgressDto(string Stage, int Done, int? Total);
