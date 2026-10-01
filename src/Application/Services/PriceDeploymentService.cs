using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Exceptions;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Enums;
using KiotVietTool.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class PriceDeploymentService(
    IDiscountProgramRepository programs,
    IProgramPriceRepository prices,
    IDiscountProgramService programService,
    ICatalogSyncService catalogSync,
    IKiotVietConnectionRepository connections,
    IKiotVietApiService api,
    ISecretProtectorService secretProtector,
    IDatabaseBackupService backup,
    ProgramScheduleService schedule,
    TimeProvider timeProvider,
    ILogger<PriceDeploymentService> logger) : IPriceDeploymentService
{
    const string BusyMessage = "Tool đang áp giá hoặc trả giá cho một chương trình khác. Hãy thử lại sau ít phút.";

    readonly SemaphoreSlim _gate = new(1, 1);

    public bool IsRunning => _gate.CurrentCount == 0;

    public event EventHandler? Changed;

    public Task<Result<string>> ActivateAsync(int programId, IProgress<DeploymentProgressDto>? progress = null,
        CancellationToken cancellationToken = default) =>
        RunExclusiveAsync(programId, async (program, credentials) =>
        {
            if (program.Status != ProgramStatusEnum.Draft) return Result.Failure<string>("Chỉ áp dụng được chương trình nháp.");
            if (program.IsEndedAt(UtcNow)) return Result.Failure<string>("Chương trình đã quá thời điểm kết thúc.");

            if (program.StartAtUtc is { } start && start > UtcNow)
            {
                var check = await programService.PreviewSavedAsync(program.Id, cancellationToken);
                if (!check.IsSuccess) return Result.Failure<string>(check.Error!);
                if (check.Value!.ConflictCount > 0) return Result.Failure<string>(ConflictMessage(check.Value.ConflictCount));
                program.Schedule(UtcNow);
                await prices.SaveAsync(program, [], CancellationToken.None);
                await schedule.SyncAsync(program, CancellationToken.None);
                logger.LogInformation("Program {ProgramId} scheduled to start at {Start:o}", program.Id, start);
                return Result.Success($"Đã lên lịch. Tool sẽ tự đổi giá lúc {VietnamTime.FromUtc(start):dd/MM/yyyy HH:mm}.");
            }

            return await ApplyAsync(program, credentials, progress, cancellationToken);
        }, cancellationToken);

    public Task<Result<string>> StopAsync(int programId, IProgress<DeploymentProgressDto>? progress = null,
        CancellationToken cancellationToken = default) =>
        RunExclusiveAsync(programId, async (program, credentials) =>
        {
            if (program.Status == ProgramStatusEnum.Scheduled)
            {
                program.Unschedule(UtcNow);
                await prices.SaveAsync(program, [], CancellationToken.None);
                await schedule.RemoveAsync(program.Id, CancellationToken.None);
                return Result.Success("Đã huỷ lịch. Chương trình trở về nháp.");
            }
            if (!program.HoldsKiotVietPrices) return Result.Failure<string>("Chương trình không giữ giá giảm nào trên KiotViet.");
            return await RestoreAsync(program, credentials, stopEarly: true, progress, cancellationToken);
        }, cancellationToken);

    public Task<Result<string>> RetryAsync(int programId, IProgress<DeploymentProgressDto>? progress = null,
        CancellationToken cancellationToken = default) =>
        RunExclusiveAsync(programId, (program, credentials) => program.Status switch
        {
            ProgramStatusEnum.ApplyFailed or ProgramStatusEnum.Applying when !program.IsEndedAt(UtcNow) =>
                ApplyAsync(program, credentials, progress, cancellationToken),
            ProgramStatusEnum.ApplyFailed or ProgramStatusEnum.Applying or ProgramStatusEnum.Restoring or ProgramStatusEnum.RestoreFailed =>
                RestoreAsync(program, credentials, program.IsStopRequested, progress, cancellationToken),
            _ => Task.FromResult(Result.Failure<string>("Chương trình không có việc nào cần thử lại.")),
        }, cancellationToken);

    public Task<Result<string>> ChangeEndAsync(int programId, DateTime endAtUtc, CancellationToken cancellationToken = default) =>
        RunExclusiveAsync(programId, async (program, credentials) =>
        {
            if (endAtUtc <= UtcNow && program.HoldsKiotVietPrices)
                return await RestoreAsync(program, credentials, stopEarly: true, null, cancellationToken);

            var oldEnd = program.EndAtUtc;
            try { program.ChangeEnd(endAtUtc, UtcNow); }
            catch (DomainException ex) { return Result.Failure<string>(ex.Message); }

            await programs.UpdateAsync(program, CancellationToken.None);
            var check = await programService.PreviewSavedAsync(program.Id, cancellationToken);
            if (check.Value is { ConflictCount: > 0 } conflicting)
            {
                program.ChangeEnd(oldEnd, UtcNow);
                await programs.UpdateAsync(program, CancellationToken.None);
                return Result.Failure<string>(ConflictMessage(conflicting.ConflictCount));
            }
            await schedule.SyncAsync(program, CancellationToken.None);
            logger.LogInformation("Program {ProgramId} end changed from {Old:o} to {New:o}", program.Id, oldEnd, program.EndAtUtc);
            return Result.Success($"Đã đổi thời điểm kết thúc thành {VietnamTime.FromUtc(program.EndAtUtc):dd/MM/yyyy HH:mm}.");
        }, cancellationToken);

    public Task<Result<string>> ResolveManualChangesAsync(int programId, bool restoreOriginal, CancellationToken cancellationToken = default) =>
        RunExclusiveAsync(programId, async (program, credentials) =>
        {
            var items = await prices.GetAsync(program.Id, cancellationToken);
            var changed = items.Where(i => i.State == PriceStateEnum.ChangedManually).ToList();
            if (changed.Count == 0) return Result.Failure<string>("Không có sản phẩm nào bị sửa giá tay.");

            if (!restoreOriginal)
            {
                foreach (var item in changed) item.MarkKept(UtcNow);
                await prices.SaveAsync(program, items, CancellationToken.None);
                logger.LogInformation("Program {ProgramId}: kept {Count} manually changed prices", program.Id, changed.Count);
                return Result.Success($"Đã giữ giá hiện tại trên KiotViet cho {changed.Count} sản phẩm.");
            }

            var failures = await WriteAsync(program, credentials, items.ToList(), changed, i => i.OriginalPrice, i => i.MarkRestored(UtcNow),
                (i, error) => i.MarkRestoreFailed(error), "Trả về giá gốc", null, cancellationToken);
            logger.LogInformation("Program {ProgramId}: restored {Count} manually changed prices ({Failures} failed)",
                program.Id, changed.Count, failures);
            return failures == 0
                ? Result.Success($"Đã trả về giá gốc cho {changed.Count} sản phẩm.")
                : Result.Failure<string>($"{failures} sản phẩm chưa trả được giá gốc. Bấm lại để thử tiếp.");
        }, cancellationToken);

    public async Task<int> RunDueAsync(CancellationToken cancellationToken = default)
    {
        if (!await _gate.WaitAsync(0, cancellationToken)) return 0;
        var handled = 0;
        try
        {
            var now = UtcNow;
            var due = (await programs.GetAllAsync(cancellationToken)).Where(p => p.Status switch
            {
                ProgramStatusEnum.Applying or ProgramStatusEnum.Restoring or ProgramStatusEnum.RestoreFailed => true,
                ProgramStatusEnum.Scheduled => p.IsStartDueAt(now),
                ProgramStatusEnum.Running or ProgramStatusEnum.ApplyFailed => p.IsEndedAt(now),
                _ => false,
            }).ToList();
            if (due.Count == 0) return 0;

            var credentials = await LoadCredentialsAsync(cancellationToken);
            if (!credentials.IsSuccess)
            {
                logger.LogWarning("Due price jobs skipped: {Error}", credentials.Error);
                return 0;
            }

            foreach (var program in due)
            {
                var result = await RunDueProgramAsync(program, credentials.Value!, cancellationToken);
                handled++;
                logger.LogInformation("Due job for program {ProgramId}: {Result}", program.Id, result.IsSuccess ? result.Value : result.Error);
            }
            return handled;
        }
        finally
        {
            _gate.Release();
            if (handled > 0) Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    async Task<Result<string>> RunDueProgramAsync(DiscountProgram program, KiotVietCredentialsDto credentials, CancellationToken cancellationToken)
    {
        var now = UtcNow;
        switch (program.Status)
        {
            case ProgramStatusEnum.Scheduled when program.IsEndedAt(now):
                program.EndWithoutApplying(now);
                await prices.SaveAsync(program, [], CancellationToken.None);
                await schedule.RemoveAsync(program.Id, CancellationToken.None);
                logger.LogWarning("Program {ProgramId} ended without applying: the tool was not running during the whole program", program.Id);
                return Result.Success("Không áp giá vì máy không chạy trong thời gian chương trình.");
            case ProgramStatusEnum.Scheduled:
            case ProgramStatusEnum.Applying when !program.IsEndedAt(now):
                return await ApplyAsync(program, credentials, null, cancellationToken);
            default:
                return await RestoreAsync(program, credentials, program.IsStopRequested, null, cancellationToken);
        }
    }

    async Task<Result<string>> ApplyAsync(DiscountProgram program, KiotVietCredentialsDto credentials,
        IProgress<DeploymentProgressDto>? progress, CancellationToken cancellationToken)
    {
        var items = (await prices.GetAsync(program.Id, cancellationToken)).ToList();
        if (items.Count == 0)
        {
            progress?.Report(new DeploymentProgressDto("Đồng bộ danh mục từ KiotViet", 0, 0));
            var synced = await catalogSync.SyncAsync(null, cancellationToken);
            if (!synced.IsSuccess) return Result.Failure<string>(synced.Error!);

            var preview = await programService.PreviewSavedAsync(program.Id, cancellationToken);
            if (!preview.IsSuccess) return Result.Failure<string>(preview.Error!);
            if (preview.Value!.ConflictCount > 0) return Result.Failure<string>(ConflictMessage(preview.Value.ConflictCount));

            progress?.Report(new DeploymentProgressDto("Đọc giá gốc trên KiotViet", 0, 0));
            var originals = await api.GetBasePricesAsync(credentials, cancellationToken);
            foreach (var row in preview.Value.Rows.Where(r => r.DiscountedPrice is not null))
            {
                if (!originals.TryGetValue(row.ProductId, out var original)) continue;
                var discounted = DiscountProgram.CalculateDiscountedPrice(original, program.Type, program.Value, program.Rounding);
                if (discounted <= 0 || discounted >= original) continue;
                items.Add(ProgramProductPrice.Create(program.Id, row.ProductId, row.Code, row.FullName, original, discounted));
            }
            if (items.Count == 0) return Result.Failure<string>("Không có sản phẩm nào được giảm giá trong phạm vi chương trình.");
        }

        try
        {
            var backupPath = await backup.BackupAsync($"apply-{program.Id}", cancellationToken);
            logger.LogInformation("Database backed up to {Path} before applying program {ProgramId}", backupPath, program.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Backup before applying program {ProgramId} failed; nothing was written to KiotViet", program.Id);
            return Result.Failure<string>("Không sao lưu được dữ liệu trước khi áp giá nên tool chưa đổi giá nào. Kiểm tra dung lượng ổ đĩa rồi thử lại.");
        }

        try { program.BeginApplying(UtcNow); }
        catch (DomainException ex) { return Result.Failure<string>(ex.Message); }
        await prices.SaveAsync(program, items, CancellationToken.None);

        try
        {
            progress?.Report(new DeploymentProgressDto("Đọc giá hiện tại trên KiotViet", 0, items.Count));
            var live = await api.GetBasePricesAsync(credentials, cancellationToken);
            var toWrite = new List<ProgramProductPrice>();
            foreach (var item in items.Where(i => i.NeedsApply))
            {
                if (!live.TryGetValue(item.ProductId, out var current)) item.MarkApplyFailed("Sản phẩm đã bị xoá trên KiotViet.");
                else if (current == item.DiscountedPrice) item.MarkApplied(UtcNow);
                else if (current == item.OriginalPrice) toWrite.Add(item);
                else item.MarkApplyFailed($"Giá trên KiotViet vừa đổi thành {current:#,##0} ₫, khác giá gốc đã lưu {item.OriginalPrice:#,##0} ₫. Dừng chương trình rồi tạo lại để áp theo giá mới.");
            }
            await prices.SaveAsync(program, items, CancellationToken.None);

            await WriteAsync(program, credentials, items, toWrite, i => i.DiscountedPrice, i => i.MarkApplied(UtcNow),
                (i, error) => i.MarkApplyFailed(error), "Đổi giá trên KiotViet", progress, cancellationToken);

            progress?.Report(new DeploymentProgressDto("Kiểm tra lại giá trên KiotViet", items.Count, items.Count));
            live = await api.GetBasePricesAsync(credentials, cancellationToken);
            foreach (var item in items.Where(i => i.State == PriceStateEnum.Applied
                         && (!live.TryGetValue(i.ProductId, out var current) || current != i.DiscountedPrice)))
                item.MarkApplyFailed("Đọc lại giá trên KiotViet không khớp giá giảm.");
        }
        catch (Exception ex) when (ex is KiotVietApiException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Applying program {ProgramId} interrupted", program.Id);
        }

        var failed = items.Count(i => i.NeedsApply);
        program.FinishApplying(failed == 0, UtcNow);
        await prices.SaveAsync(program, items, CancellationToken.None);
        await TrySyncScheduleAsync(program);
        logger.LogInformation("Program {ProgramId} applied: {Applied}/{Total} products, {Failed} failed",
            program.Id, items.Count(i => i.State == PriceStateEnum.Applied), items.Count, failed);

        var applied = items.Count(i => i.State == PriceStateEnum.Applied);
        return failed == 0
            ? Result.Success($"Đã đổi giá {applied} sản phẩm trên KiotViet. Tool sẽ trả giá gốc lúc {VietnamTime.FromUtc(program.EndAtUtc):dd/MM/yyyy HH:mm}.")
            : Result.Failure<string>($"Đã đổi giá {applied}/{items.Count} sản phẩm. {failed} sản phẩm lỗi — bấm Thử lại.");
    }

    async Task<Result<string>> RestoreAsync(DiscountProgram program, KiotVietCredentialsDto credentials, bool stopEarly,
        IProgress<DeploymentProgressDto>? progress, CancellationToken cancellationToken)
    {
        var items = (await prices.GetAsync(program.Id, cancellationToken)).ToList();
        program.BeginRestoring(stopEarly, UtcNow);
        await prices.SaveAsync(program, items, CancellationToken.None);

        var unfinished = items.Where(i => i.State is PriceStateEnum.Applied or PriceStateEnum.Pending or PriceStateEnum.Failed).ToList();
        try
        {
            progress?.Report(new DeploymentProgressDto("Đọc giá hiện tại trên KiotViet", 0, unfinished.Count));
            var live = await api.GetBasePricesAsync(credentials, cancellationToken);
            var toWrite = new List<ProgramProductPrice>();
            foreach (var item in unfinished)
            {
                if (!live.TryGetValue(item.ProductId, out var current) || current == item.OriginalPrice) item.MarkRestored(UtcNow);
                else if (current == item.DiscountedPrice) toWrite.Add(item);
                else if (item.State == PriceStateEnum.Applied) item.MarkChangedManually(current);
                else item.MarkRestored(UtcNow);
            }
            await prices.SaveAsync(program, items, CancellationToken.None);

            await WriteAsync(program, credentials, items, toWrite, i => i.OriginalPrice, i => i.MarkRestored(UtcNow),
                (i, error) => i.MarkRestoreFailed(error), "Trả giá gốc trên KiotViet", progress, cancellationToken);

            progress?.Report(new DeploymentProgressDto("Kiểm tra lại giá trên KiotViet", unfinished.Count, unfinished.Count));
            live = await api.GetBasePricesAsync(credentials, cancellationToken);
            foreach (var item in toWrite.Where(i => i.State == PriceStateEnum.Restored
                         && live.TryGetValue(i.ProductId, out var current) && current != i.OriginalPrice))
                item.MarkRestoreFailed("Đọc lại giá trên KiotViet không khớp giá gốc.");
        }
        catch (Exception ex) when (ex is KiotVietApiException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Restoring program {ProgramId} interrupted", program.Id);
        }

        var pending = items.Count(i => i.State is PriceStateEnum.Applied or PriceStateEnum.Pending or PriceStateEnum.Failed
            || (i.State == PriceStateEnum.Restored && i.LastError is not null));
        program.FinishRestoring(pending == 0, UtcNow);
        await prices.SaveAsync(program, items, CancellationToken.None);
        await TrySyncScheduleAsync(program);

        var restored = items.Count(i => i.State == PriceStateEnum.Restored && i.LastError is null);
        var manual = items.Count(i => i.State == PriceStateEnum.ChangedManually);
        logger.LogInformation("Program {ProgramId} restored: {Restored} products, {Manual} changed manually, {Pending} pending",
            program.Id, restored, manual, pending);

        if (pending > 0) return Result.Failure<string>($"Còn {pending} sản phẩm chưa trả được giá gốc — bấm Thử lại.");
        var message = $"Đã trả giá gốc cho {restored} sản phẩm.";
        if (manual > 0) message += $" {manual} sản phẩm đã bị sửa giá trên KiotViet nên tool không ghi đè — hãy chọn giữ giá hiện tại hoặc trả về giá gốc.";
        return Result.Success(message);
    }

    async Task<int> WriteAsync(DiscountProgram program, KiotVietCredentialsDto credentials, List<ProgramProductPrice> all, IReadOnlyList<ProgramProductPrice> targets,
        Func<ProgramProductPrice, decimal> priceOf, Action<ProgramProductPrice> onSuccess, Action<ProgramProductPrice, string> onError,
        string stage, IProgress<DeploymentProgressDto>? progress, CancellationToken cancellationToken)
    {
        var failures = 0;
        var done = 0;
        foreach (var batch in targets.Chunk(Math.Max(1, api.PriceBatchSize)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await api.UpdateBasePricesAsync(credentials, [.. batch.Select(i => new ProductPriceUpdateDto(i.ProductId, priceOf(i)))], cancellationToken);
                foreach (var item in batch) onSuccess(item);
            }
            catch (KiotVietApiException batchError)
            {
                logger.LogWarning(batchError, "Batch price update failed for program {ProgramId}; retrying one by one", program.Id);
                foreach (var item in batch)
                {
                    try
                    {
                        await api.UpdateBasePriceAsync(credentials, new ProductPriceUpdateDto(item.ProductId, priceOf(item)), cancellationToken);
                        onSuccess(item);
                    }
                    catch (KiotVietApiException itemError)
                    {
                        onError(item, itemError.Message);
                        failures++;
                    }
                }
            }
            done += batch.Length;
            await prices.SaveAsync(program, all, CancellationToken.None);
            progress?.Report(new DeploymentProgressDto(stage, done, targets.Count));
        }
        return failures;
    }

    async Task<Result<string>> RunExclusiveAsync(int programId, Func<DiscountProgram, KiotVietCredentialsDto, Task<Result<string>>> action,
        CancellationToken cancellationToken)
    {
        if (!await _gate.WaitAsync(0, cancellationToken)) return Result.Failure<string>(BusyMessage);
        try
        {
            var program = await programs.GetAsync(programId, cancellationToken);
            if (program is null) return Result.Failure<string>("Không tìm thấy chương trình.");
            var credentials = await LoadCredentialsAsync(cancellationToken);
            if (!credentials.IsSuccess) return Result.Failure<string>(credentials.Error!);
            return await action(program, credentials.Value!);
        }
        catch (KiotVietApiException ex)
        {
            logger.LogWarning(ex, "Price deployment for program {ProgramId} failed", programId);
            return Result.Failure<string>(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.InnerException is not null)
        {
            return Result.Failure<string>(ex.Message);
        }
        finally
        {
            _gate.Release();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    async Task<Result<KiotVietCredentialsDto>> LoadCredentialsAsync(CancellationToken cancellationToken)
    {
        var connection = await connections.GetAsync(cancellationToken);
        if (connection is null) return Result.Failure<KiotVietCredentialsDto>("Chưa kết nối KiotViet.");
        var secret = secretProtector.Unprotect(connection.EncryptedClientSecret);
        if (string.IsNullOrEmpty(secret))
            return Result.Failure<KiotVietCredentialsDto>("Không đọc được Client Secret đã lưu. Hãy nhập lại Client Secret trong Kết nối KiotViet.");
        return Result.Success(new KiotVietCredentialsDto(connection.Retailer, connection.ClientId, secret));
    }

    async Task TrySyncScheduleAsync(DiscountProgram program)
    {
        try { await schedule.SyncAsync(program, CancellationToken.None); }
        catch (InvalidOperationException ex) { logger.LogWarning(ex, "Scheduled task update failed for program {ProgramId}", program.Id); }
    }

    DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    static string ConflictMessage(int count) =>
        $"{count} sản phẩm đang thuộc chương trình khác trong cùng thời gian. Mở chương trình, loại trừ các sản phẩm này hoặc đổi thời gian.";
}
