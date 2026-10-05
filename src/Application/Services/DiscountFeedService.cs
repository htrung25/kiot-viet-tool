using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Exceptions;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Enums;
using KiotVietTool.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class DiscountFeedService(
    IDiscountProgramRepository programs,
    IDiscountProgramService programService,
    IDiscountFeedApiService feedApi,
    TimeProvider timeProvider,
    ILogger<DiscountFeedService> logger) : IDiscountFeedService
{
    const string NotConfiguredMessage = "Chưa cấu hình kết nối tới máy thu ngân (mục DiscountFeed trong appsettings.json).";

    readonly SemaphoreSlim _gate = new(1, 1);
    string? _publishedHash;

    public DiscountFeedStatusDto Status { get; private set; } = new(feedApi.IsConfigured, null, null);

    public event EventHandler? Changed;

    public Task<Result<string>> ActivateAsync(int programId, CancellationToken cancellationToken = default) =>
        ChangeProgramAsync(programId, async program =>
        {
            var check = await programService.PreviewSavedAsync(program.Id, cancellationToken);
            if (!check.IsSuccess) return check.Error;
            if (check.Value!.AppliedCount == 0) return "Không có sản phẩm nào được giảm giá trong phạm vi chương trình.";
            if (check.Value.ConflictCount > 0) return ConflictMessage(check.Value.ConflictCount);
            program.Publish(UtcNow);
            return null;
        }, program => program.PhaseAt(UtcNow) == ProgramPhaseEnum.Upcoming
            ? $"Đã áp dụng. Máy thu ngân tự giảm giá từ {VietnamTime.FromUtc(program.StartAtUtc!.Value):dd/MM/yyyy HH:mm}."
            : "Đã áp dụng. Máy thu ngân bắt đầu giảm giá trong khoảng 1–2 phút.",
            cancellationToken);

    public Task<Result<string>> StopAsync(int programId, CancellationToken cancellationToken = default) =>
        ChangeProgramAsync(programId, program =>
        {
            program.Stop(UtcNow);
            return Task.FromResult<string?>(null);
        }, program => program.IsDraft
            ? "Đã huỷ áp dụng. Chương trình trở về nháp."
            : "Đã dừng. Máy thu ngân ngừng giảm giá trong khoảng 1–2 phút.",
            cancellationToken);

    public Task<Result<string>> ChangeEndAsync(int programId, DateTime endAtUtc, CancellationToken cancellationToken = default) =>
        ChangeProgramAsync(programId, async program =>
        {
            var oldEnd = program.EndAtUtc;
            program.ChangeEnd(endAtUtc, UtcNow);
            await programs.UpdateAsync(program, cancellationToken);
            var check = await programService.PreviewSavedAsync(program.Id, cancellationToken);
            if (check.Value is not { ConflictCount: > 0 } conflicting) return null;
            program.ChangeEnd(oldEnd, UtcNow);
            await programs.UpdateAsync(program, CancellationToken.None);
            return ConflictMessage(conflicting.ConflictCount);
        }, program => $"Đã đổi thời điểm kết thúc thành {VietnamTime.FromUtc(program.EndAtUtc):dd/MM/yyyy HH:mm}.",
            cancellationToken);

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await PublishIfChangedAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    async Task<Result<string>> ChangeProgramAsync(int programId, Func<DiscountProgram, Task<string?>> change,
        Func<DiscountProgram, string> successMessage, CancellationToken cancellationToken)
    {
        if (!feedApi.IsConfigured) return Result.Failure<string>(NotConfiguredMessage);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var program = await programs.GetAsync(programId, cancellationToken);
            if (program is null) return Result.Failure<string>("Không tìm thấy chương trình.");
            try
            {
                if (await change(program) is { } error) return Result.Failure<string>(error);
            }
            catch (DomainException ex)
            {
                return Result.Failure<string>(ex.Message);
            }
            await programs.UpdateAsync(program, CancellationToken.None);
            logger.LogInformation("Program {ProgramId} is now {Status}, ends {End:o}", program.Id, program.Status, program.EndAtUtc);

            await PublishIfChangedAsync(cancellationToken);
            return Status.LastError is { } publishError
                ? Result.Failure<string>($"Đã lưu trong tool nhưng chưa gửi được tới máy thu ngân: {publishError} Tool tự gửi lại mỗi phút.")
                : Result.Success(successMessage(program));
        }
        finally
        {
            _gate.Release();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    async Task PublishIfChangedAsync(CancellationToken cancellationToken)
    {
        if (!feedApi.IsConfigured)
        {
            SetStatus(new DiscountFeedStatusDto(false, null, null));
            return;
        }

        var feedPrograms = await programService.GetFeedProgramsAsync(cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(feedPrograms))));
        if (hash == _publishedHash && Status.LastError is null) return;

        var now = UtcNow;
        try
        {
            await feedApi.PublishAsync(new DiscountFeedDto(new DateTimeOffset(now).ToUnixTimeMilliseconds(), now, feedPrograms),
                cancellationToken);
            _publishedHash = hash;
            SetStatus(new DiscountFeedStatusDto(true, now, null));
            logger.LogInformation("Discount feed published: {Programs} programs, {Products} products", feedPrograms.Count,
                feedPrograms.Sum(p => p.ProductIds.Count));
        }
        catch (DiscountFeedException ex)
        {
            logger.LogWarning(ex, "Publishing the discount feed failed");
            SetStatus(Status with { IsConfigured = true, LastError = ex.Message });
        }
    }

    void SetStatus(DiscountFeedStatusDto status)
    {
        if (status == Status) return;
        Status = status;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    static string ConflictMessage(int count) =>
        $"{count} sản phẩm đang thuộc chương trình khác trong cùng thời gian. Mở chương trình, loại trừ các sản phẩm này hoặc đổi thời gian.";
}
