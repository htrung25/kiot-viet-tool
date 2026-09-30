namespace KiotVietTool.Application.DTOs;

public sealed record KiotVietConnectionDto(string Retailer, string ClientId, DateTime? LastSyncedAtUtc);
