namespace KiotVietTool.Application.DTOs;

public sealed record CloudflareWorkerDeployDto(string ApiToken, string AccountId, string ScriptName, string WriteToken, string ReadToken);
