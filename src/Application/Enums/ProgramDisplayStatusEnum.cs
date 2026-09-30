namespace KiotVietTool.Application.Enums;

public enum ProgramDisplayStatusEnum
{
    Draft = 1,
    Deploying = 2,
    Scheduled = 3,
    Running = 4,
    Ended = 5,
    NeedsRedeploy = 6,
    DeployFailed = 7,
    StopFailed = 8,
    Cancelled = 9,
}
