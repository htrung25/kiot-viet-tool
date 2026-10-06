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
    IDiscountFeedConnectionRepository connections,
    IDiscountFeedSnapshotRepository snapshots,
    IDiscountFeedApiService feedApi,
    ISecretProtectorService secretProtector,
    TimeProvider timeProvider,
    ILogger<DiscountFeedService> logger) : IDiscountFeedService
{
    const string NotConfiguredMessage = "Chưa kết nối máy thu ngân. Vào Hệ thống → Máy thu ngân để cài Worker giảm giá.";
    const string UnreadableTokenMessage = "Không đọc được mã ghi đã lưu (dữ liệu có thể được khôi phục từ máy hoặc user Windows khác). Hãy kết nối lại ở mục Máy thu ngân.";
    const string ConflictMessage = "Danh sách giảm giá trên Worker đã được máy khác (hoặc bản dữ liệu khác của tool) cập nhật, nên tool tạm ngừng gửi để không ghi đè. Vào Hệ thống → Máy thu ngân để xử lý.";

    readonly SemaphoreSlim _gate = new(1, 1);
    // Hashes PUT since the last confirmed write: a 412 showing one of them came from our own write whose response was lost.
    readonly HashSet<string> _attemptedHashes = [];
    string? _publishedHash;

    public DiscountFeedStatusDto Status { get; private set; } = new(false, null, null);

    public event EventHandler? Changed;

    public Task<Result<string>> ActivateAsync(int programId, CancellationToken cancellationToken = default) =>
        ChangeProgramAsync(programId, async program =>
        {
            var check = await programService.PreviewSavedAsync(program.Id, cancellationToken);
            if (!check.IsSuccess) return check.Error;
            if (check.Value!.AppliedCount == 0) return "Không có sản phẩm nào được giảm giá trong phạm vi chương trình.";
            if (check.Value.ConflictCount > 0) return ProgramConflictMessage(check.Value.ConflictCount);
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
            return ProgramConflictMessage(conflicting.ConflictCount);
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

    public async Task<Result<string>> TakeOverAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (await connections.GetAsync(cancellationToken) is not { } connection) return Result.Failure<string>(NotConfiguredMessage);
            if (WriteEndpoint(connection) is not { } endpoint) return Result.Failure<string>(UnreadableTokenMessage);

            DiscountFeedRemoteStateDto remote;
            try
            {
                remote = await feedApi.GetStateAsync(endpoint, cancellationToken);
            }
            catch (DiscountFeedException ex)
            {
                return Result.Failure<string>(ex.Message);
            }

            await connections.UpdateRevisionAsync(connection.Id, connection.InstanceId, remote.Revision, cancellationToken);
            logger.LogWarning("Taking over the discount feed at revision {Revision} (last written by {Writer})", remote.Revision,
                remote.InstanceId == connection.InstanceId ? "this installation" : "another installation");
            _publishedHash = null;
            SetStatus(Status with { Conflict = null, LastError = null });

            await PublishIfChangedAsync(cancellationToken);
            return Status switch
            {
                { Conflict: not null } => Result.Failure<string>("Worker vừa được máy khác cập nhật thêm một lần nữa. Kiểm tra máy kia rồi thử lại."),
                { LastError: { } error } => Result.Failure<string>(error),
                _ => Result.Success("Đã ghi đè danh sách giảm giá trên Worker bằng dữ liệu của máy này."),
            };
        }
        finally
        {
            _gate.Release();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<Result<string>> ChangeConnectionAsync(Func<CancellationToken, Task<Result<string>>> change,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var result = await change(cancellationToken);
            if (!result.IsSuccess) return result;

            _publishedHash = null;
            _attemptedHashes.Clear();
            SetStatus(new DiscountFeedStatusDto(false, null, null));
            await PublishIfChangedAsync(CancellationToken.None);
            return result;
        }
        finally
        {
            _gate.Release();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    async Task<Result<string>> ChangeProgramAsync(int programId, Func<DiscountProgram, Task<string?>> change,
        Func<DiscountProgram, string> successMessage, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (await connections.GetAsync(cancellationToken) is null) return Result.Failure<string>(NotConfiguredMessage);
            if (Status.Conflict is not null) return Result.Failure<string>(ConflictMessage);

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
            return Status switch
            {
                { Conflict: not null } => Result.Failure<string>($"Đã lưu trong tool nhưng chưa gửi tới máy thu ngân. {ConflictMessage}"),
                { LastError: { } publishError } => Result.Failure<string>(
                    $"Đã lưu trong tool nhưng chưa gửi được tới máy thu ngân: {publishError} Tool tự gửi lại mỗi phút."),
                _ => Result.Success(successMessage(program)),
            };
        }
        finally
        {
            _gate.Release();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    async Task PublishIfChangedAsync(CancellationToken cancellationToken)
    {
        var connection = await connections.GetAsync(cancellationToken);
        if (connection is null)
        {
            SetStatus(new DiscountFeedStatusDto(false, null, null));
            return;
        }
        // A conflict stays until the user takes over or changes the connection: retrying could not succeed.
        if (Status.Conflict is not null) return;
        if (WriteEndpoint(connection) is not { } endpoint)
        {
            SetStatus(Status with { IsConfigured = true, LastError = UnreadableTokenMessage });
            return;
        }

        var feedPrograms = await programService.GetFeedProgramsAsync(cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(feedPrograms))));
        if (hash == _publishedHash && Status is { IsConfigured: true, LastError: null })
        {
            await CheckRemoteAsync(endpoint, connection, cancellationToken);
            return;
        }

        var now = UtcNow;
        var feed = new DiscountFeedDto(new DateTimeOffset(now).ToUnixTimeMilliseconds(), now, connection.InstanceId, hash, feedPrograms);
        _attemptedHashes.Add(hash);
        try
        {
            long revision;
            try
            {
                revision = await feedApi.PublishAsync(endpoint, feed, connection.LastRevision, cancellationToken);
            }
            catch (DiscountFeedConflictException ex) when (IsOwnUnconfirmedWrite(ex.Current, connection))
            {
                logger.LogInformation("Discount feed revision {Revision} is an earlier write of this installation; continuing from it",
                    ex.Current.Revision);
                revision = ex.Current.ContentHash == hash
                    ? ex.Current.Revision
                    : await feedApi.PublishAsync(endpoint, feed, ex.Current.Revision, cancellationToken);
            }

            await connections.UpdateRevisionAsync(connection.Id, connection.InstanceId, revision, CancellationToken.None);
            await snapshots.AddAsync(DiscountFeedSnapshot.Create(revision, now, feedPrograms.Select(p =>
                new CheckoutProgram(p.Id, p.Name, p.Type, p.Value, p.StartAtUtc, p.EndAtUtc, p.ProductIds))), CancellationToken.None);
            _publishedHash = hash;
            _attemptedHashes.Clear();
            SetStatus(new DiscountFeedStatusDto(true, now, null));
            logger.LogInformation("Discount feed published as revision {Revision}: {Programs} programs, {Products} products", revision,
                feedPrograms.Count, feedPrograms.Sum(p => p.ProductIds.Count));
        }
        catch (DiscountFeedConflictException ex)
        {
            var byOther = ex.Current.InstanceId != connection.InstanceId;
            logger.LogWarning("Discount feed not published: the Worker is at revision {Remote} by {Writer}, this installation expected {Local}",
                ex.Current.Revision, byOther ? "another installation" : "this installation", connection.LastRevision);
            SetStatus(new DiscountFeedStatusDto(true, Status.LastPublishedAtUtc, null,
                new DiscountFeedConflictDto(ex.Current.Revision, byOther, ex.Current.GeneratedAtUtc)));
        }
        catch (DiscountFeedException ex)
        {
            logger.LogWarning(ex, "Publishing the discount feed failed");
            SetStatus(Status with { IsConfigured = true, LastError = ex.Message });
        }
    }

    // Nothing to send: still look at the Worker, so this installation notices when another one took over the feed
    // instead of finding out only at its next change.
    async Task CheckRemoteAsync(DiscountFeedEndpointDto endpoint, DiscountFeedConnection connection, CancellationToken cancellationToken)
    {
        DiscountFeedRemoteStateDto remote;
        try
        {
            remote = await feedApi.GetStateAsync(endpoint, cancellationToken);
        }
        catch (DiscountFeedException ex)
        {
            logger.LogWarning(ex, "Checking the discount feed on the Worker failed");
            SetStatus(Status with { LastError = ex.Message });
            return;
        }
        if (remote.Revision == connection.LastRevision) return;

        logger.LogWarning("Discount feed on the Worker moved to revision {Remote} by {Writer}; this installation is at {Local}",
            remote.Revision, remote.InstanceId == connection.InstanceId ? "this installation" : "another installation", connection.LastRevision);
        SetStatus(Status with
        {
            Conflict = new DiscountFeedConflictDto(remote.Revision, remote.InstanceId != connection.InstanceId, remote.GeneratedAtUtc),
        });
    }

    // Our PUT reached the Worker but its response did not reach us (timeout, network drop): the Worker is ahead,
    // written by this installation with content we sent in this session.
    bool IsOwnUnconfirmedWrite(DiscountFeedRemoteStateDto remote, DiscountFeedConnection connection) =>
        remote.InstanceId == connection.InstanceId && remote.Revision > connection.LastRevision
        && remote.ContentHash is { } remoteHash && _attemptedHashes.Contains(remoteHash);

    DiscountFeedEndpointDto? WriteEndpoint(DiscountFeedConnection connection) =>
        secretProtector.Unprotect(connection.EncryptedWriteToken) is { Length: > 0 } token
            ? new DiscountFeedEndpointDto(connection.WorkerUrl, token)
            : null;

    void SetStatus(DiscountFeedStatusDto status)
    {
        if (status == Status) return;
        Status = status;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    static string ProgramConflictMessage(int count) =>
        $"{count} sản phẩm đang thuộc chương trình khác trong cùng thời gian. Mở chương trình, loại trừ các sản phẩm này hoặc đổi thời gian.";
}
