using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Enums;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class ProgramScheduleService(
    ITaskSchedulerService scheduler,
    IKiotVietApiService api,
    IServerClockService clock,
    ILogger<ProgramScheduleService> logger)
{
    public async Task SyncAsync(DiscountProgram program, CancellationToken cancellationToken)
    {
        try
        {
            await api.SyncClockAsync(cancellationToken);
            if (program.Status == ProgramStatusEnum.Scheduled && program.StartAtUtc is { } start)
                await scheduler.ScheduleAsync(StartTaskName(program.Id), clock.ToMachineUtc(start), cancellationToken);
            else
                await scheduler.RemoveAsync(StartTaskName(program.Id), cancellationToken);

            if (program.Status is ProgramStatusEnum.Scheduled || program.HoldsKiotVietPrices)
                await scheduler.ScheduleAsync(EndTaskName(program.Id), clock.ToMachineUtc(program.EndAtUtc), cancellationToken);
            else
                await scheduler.RemoveAsync(EndTaskName(program.Id), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not update Windows scheduled tasks for program {ProgramId}", program.Id);
            throw new InvalidOperationException("Không đăng ký được lịch tự động với Windows. Hãy để tool mở vào giờ kết thúc hoặc bấm Dừng thủ công.", ex);
        }
    }

    public async Task RemoveAsync(int programId, CancellationToken cancellationToken)
    {
        try
        {
            await scheduler.RemoveAsync(StartTaskName(programId), cancellationToken);
            await scheduler.RemoveAsync(EndTaskName(programId), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not remove Windows scheduled tasks for program {ProgramId}", programId);
        }
    }

    static string StartTaskName(int programId) => $"Program-{programId}-Start";
    static string EndTaskName(int programId) => $"Program-{programId}-End";
}
