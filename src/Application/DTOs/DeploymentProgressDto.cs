namespace KiotVietTool.Application.DTOs;

public sealed record DeploymentProgressDto(string Stage, int Done, int Total);
