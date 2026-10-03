using System.Diagnostics;
using System.Security;

using KiotVietTool.Application.Interfaces;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Infrastructure.Services;

public sealed class WindowsTaskSchedulerService(ILogger<WindowsTaskSchedulerService> logger) : ITaskSchedulerService
{
    public const string RunScheduledArgument = "--run-scheduled";
    const string Folder = @"KiotVietTool\";

    public async Task ScheduleAsync(string name, DateTime runAtUtc, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            logger.LogInformation("Task Scheduler is Windows-only; skipped scheduling {Task} at {RunAt:o}", name, runAtUtc);
            return;
        }

        var xmlPath = Path.Combine(Path.GetTempPath(), $"kiotviettool-{Guid.NewGuid():N}.xml");
        try
        {
            await File.WriteAllTextAsync(xmlPath, BuildTaskXml(Environment.ProcessPath!, runAtUtc), System.Text.Encoding.Unicode, cancellationToken);
            await RunSchtasksAsync(["/Create", "/F", "/TN", Folder + name, "/XML", xmlPath], cancellationToken);
            logger.LogInformation("Scheduled Windows task {Task} at {RunAt:o}", name, runAtUtc);
        }
        finally
        {
            File.Delete(xmlPath);
        }
    }

    public async Task RemoveAsync(string name, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) return;
        await RunSchtasksAsync(["/Delete", "/F", "/TN", Folder + name], cancellationToken, ignoreMissing: true);
    }

    public static string BuildTaskXml(string exePath, DateTime runAtUtc) => $"""
        <?xml version="1.0" encoding="UTF-16"?>
        <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
          <RegistrationInfo>
            <Description>KiotViet Tool: tự áp / trả giá chương trình giảm giá đúng giờ.</Description>
          </RegistrationInfo>
          <Triggers>
            <TimeTrigger>
              <Repetition>
                <Interval>PT5M</Interval>
                <Duration>P1D</Duration>
                <StopAtDurationEnd>false</StopAtDurationEnd>
              </Repetition>
              <StartBoundary>{runAtUtc.ToUniversalTime():yyyy-MM-ddTHH:mm:ss}Z</StartBoundary>
              <Enabled>true</Enabled>
            </TimeTrigger>
          </Triggers>
          <Principals>
            <Principal id="Author">
              <LogonType>InteractiveToken</LogonType>
              <RunLevel>LeastPrivilege</RunLevel>
            </Principal>
          </Principals>
          <Settings>
            <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
            <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
            <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
            <StartWhenAvailable>true</StartWhenAvailable>
            <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
            <ExecutionTimeLimit>PT2H</ExecutionTimeLimit>
            <RestartOnFailure>
              <Interval>PT5M</Interval>
              <Count>12</Count>
            </RestartOnFailure>
            <Enabled>true</Enabled>
          </Settings>
          <Actions Context="Author">
            <Exec>
              <Command>{SecurityElement.Escape(exePath)}</Command>
              <Arguments>{RunScheduledArgument}</Arguments>
            </Exec>
          </Actions>
        </Task>
        """;

    async Task RunSchtasksAsync(string[] arguments, CancellationToken cancellationToken, bool ignoreMissing = false)
    {
        var info = new ProcessStartInfo("schtasks.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);

        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start schtasks.exe");
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0 && !ignoreMissing)
            throw new InvalidOperationException($"schtasks exited with {process.ExitCode}: {error.Trim()}");
    }
}
