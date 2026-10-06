namespace KiotVietTool.Application.DTOs;

public sealed record ReconciliationStatusDto(DateTime? LastRunAtUtc, string? LastError, int UnreviewedProblems);
