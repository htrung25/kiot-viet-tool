namespace KiotVietTool.Domain.Enums;

public enum ProgramStatusEnum
{
    Draft = 1,
    Deploying = 2,
    Deployed = 3,
    NeedsRedeploy = 4,
    DeployFailed = 5,
    StopFailed = 6,
    Cancelled = 7,
}
