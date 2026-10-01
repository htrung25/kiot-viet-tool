namespace KiotVietTool.Application.Interfaces;

public interface ITaskSchedulerService
{
    Task ScheduleAsync(string name, DateTime runAtUtc, CancellationToken cancellationToken);
    Task RemoveAsync(string name, CancellationToken cancellationToken);
}
