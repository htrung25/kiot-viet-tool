using System.Security.Cryptography;

using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Exceptions;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class DiscountFeedConnectionService(
    IDiscountFeedConnectionRepository repository,
    IDiscountFeedService feed,
    IDiscountFeedApiService feedApi,
    ICloudflareApiService cloudflare,
    IKiotVietConnectionRepository kiotVietConnections,
    IDiscountProgramRepository programs,
    ISecretProtectorService secretProtector,
    TimeProvider timeProvider,
    ILogger<DiscountFeedConnectionService> logger) : IDiscountFeedConnectionService
{
    const string WriteRole = "write";
    const string ReadRole = "read";
    const string OldWorkerMessage = "Worker này là phiên bản cũ, chưa có chống ghi đè giữa các máy. Cài lại Worker bằng nút Cài lên Cloudflare, hoặc deploy bản mới trong thư mục feed/.";

    public async Task<DiscountFeedConnectionDto?> GetAsync(CancellationToken cancellationToken = default) =>
        await repository.GetAsync(cancellationToken) is { } c
            ? new DiscountFeedConnectionDto(c.WorkerUrl, c.IsManaged, c.CloudflareAccountId, c.ScriptName,
                c.EncryptedReadToken is { } read ? secretProtector.Unprotect(read) : null, c.ConnectedAtUtc)
            : null;

    public async Task<string> SuggestScriptNameAsync(CancellationToken cancellationToken = default) =>
        await repository.GetAsync(cancellationToken) is { ScriptName: { } current }
            ? current
            : DiscountFeedConnection.SuggestScriptName((await kiotVietConnections.GetAsync(cancellationToken))?.Retailer);

    public async Task<Result<IReadOnlyList<CloudflareAccountDto>>> GetCloudflareAccountsAsync(string apiToken,
        CancellationToken cancellationToken = default)
    {
        var token = apiToken?.Trim();
        if (string.IsNullOrEmpty(token)) return Result.Failure<IReadOnlyList<CloudflareAccountDto>>("Dán Cloudflare API Token.");
        try
        {
            var accounts = await cloudflare.GetAccountsAsync(token, cancellationToken);
            return accounts.Count > 0
                ? Result.Success(accounts)
                : Result.Failure<IReadOnlyList<CloudflareAccountDto>>(
                    "API Token này không có quyền với tài khoản Cloudflare nào. Tạo token theo mẫu \"Edit Cloudflare Workers\".");
        }
        catch (CloudflareApiException ex)
        {
            return Result.Failure<IReadOnlyList<CloudflareAccountDto>>(ex.Message);
        }
    }

    public Task<Result<string>> DeployAsync(DeployDiscountFeedWorkerDto request, CancellationToken cancellationToken = default) =>
        feed.ChangeConnectionAsync(async token =>
        {
            var apiToken = request.ApiToken?.Trim();
            if (string.IsNullOrEmpty(apiToken)) return Result.Failure<string>("Dán Cloudflare API Token.");
            if (string.IsNullOrWhiteSpace(request.AccountId)) return Result.Failure<string>("Chọn tài khoản Cloudflare.");
            var scriptName = (request.ScriptName ?? "").Trim().ToLowerInvariant();
            try { DiscountFeedConnection.EnsureValidScriptName(scriptName); }
            catch (DomainException ex) { return Result.Failure<string>(ex.Message); }

            // Redeploying the same Worker keeps its tokens, so cashier extensions keep working.
            var existing = await repository.GetAsync(token);
            var sameWorker = existing is { IsManaged: true } && existing.CloudflareAccountId == request.AccountId
                && existing.ScriptName == scriptName;
            var writeToken = sameWorker ? secretProtector.Unprotect(existing!.EncryptedWriteToken) : null;
            var readToken = sameWorker && existing!.EncryptedReadToken is { } savedRead ? secretProtector.Unprotect(savedRead) : null;
            var tokensChanged = string.IsNullOrEmpty(writeToken) || string.IsNullOrEmpty(readToken);
            if (tokensChanged)
            {
                writeToken = NewToken();
                readToken = NewToken();
            }

            string url;
            try
            {
                url = await cloudflare.DeployDiscountFeedWorkerAsync(
                    new CloudflareWorkerDeployDto(apiToken, request.AccountId, scriptName, writeToken!, readToken!), token);
            }
            catch (CloudflareApiException ex)
            {
                return Result.Failure<string>(ex.Message);
            }

            var saved = await SaveAsync(existing, url, writeToken!, secretProtector.Protect(readToken!), request.AccountId, scriptName);
            if (!saved.IsSuccess) return saved;
            logger.LogInformation("Discount feed Worker {Script} deployed to {Url} ({Kind}, tokens {Tokens})", scriptName, url,
                sameWorker ? "update" : "new", tokensChanged ? "new" : "kept");
            return Result.Success(tokensChanged
                ? "Đã cài Worker lên Cloudflare. Nhập địa chỉ và mã đọc bên dưới vào tiện ích trên từng máy thu ngân. Worker mới có thể cần 1–2 phút mới chạy; tool tự gửi danh sách giảm giá."
                : "Đã cập nhật Worker trên Cloudflare. Tiện ích ở máy thu ngân không cần cấu hình lại.");
        }, cancellationToken);

    public Task<Result<string>> SaveManualAsync(SaveDiscountFeedConnectionDto request, CancellationToken cancellationToken = default) =>
        feed.ChangeConnectionAsync(async token =>
        {
            string url;
            try { url = DiscountFeedConnection.NormalizeUrl(request.WorkerUrl); }
            catch (DomainException ex) { return Result.Failure<string>(ex.Message); }

            var existing = await repository.GetAsync(token);
            var sameUrl = existing is not null && existing.WorkerUrl == url;
            var writeToken = request.WriteToken?.Trim();
            if (string.IsNullOrEmpty(writeToken) && sameUrl) writeToken = secretProtector.Unprotect(existing!.EncryptedWriteToken);
            if (string.IsNullOrEmpty(writeToken)) return Result.Failure<string>("Nhập mã ghi (WRITE_TOKEN) của Worker.");
            var readToken = request.ReadToken?.Trim();

            try
            {
                var state = await feedApi.GetStateAsync(new DiscountFeedEndpointDto(url, writeToken), token);
                if (state.TokenRole is null) return Result.Failure<string>(OldWorkerMessage);
                if (state.TokenRole != WriteRole)
                    return Result.Failure<string>("Mã vừa nhập là mã đọc. Ô Mã ghi cần WRITE_TOKEN của Worker.");
            }
            catch (DiscountFeedException ex)
            {
                return Result.Failure<string>(ex.Message);
            }

            string? encryptedRead = sameUrl ? existing!.EncryptedReadToken : null;
            if (!string.IsNullOrEmpty(readToken))
            {
                try
                {
                    var state = await feedApi.GetStateAsync(new DiscountFeedEndpointDto(url, readToken), token);
                    if (state.TokenRole != ReadRole)
                        return Result.Failure<string>("Mã đọc vừa nhập là mã ghi. Không nhập mã ghi vào máy thu ngân; ô Mã đọc cần READ_TOKEN.");
                }
                catch (DiscountFeedException)
                {
                    return Result.Failure<string>("Worker từ chối mã đọc. Kiểm tra lại READ_TOKEN.");
                }
                encryptedRead = secretProtector.Protect(readToken);
            }

            var saved = await SaveAsync(existing, url, writeToken, encryptedRead, null, null);
            if (!saved.IsSuccess) return saved;
            logger.LogInformation("Discount feed Worker {Url} connected manually", url);
            return Result.Success("Đã kết nối Worker. Tool sẽ gửi danh sách giảm giá ngay.");
        }, cancellationToken);

    public Task<Result<string>> DisconnectAsync(CancellationToken cancellationToken = default) =>
        feed.ChangeConnectionAsync(async token =>
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var active = (await programs.GetAllAsync(token)).Where(p => p.IsPublished && !p.IsEndedAt(now)).Select(p => p.Name).ToList();
            if (active.Count > 0)
                return Result.Failure<string>(
                    $"Còn chương trình đang áp dụng: {string.Join(", ", active)}. Hãy dừng các chương trình này trước, nếu không máy thu ngân vẫn giảm giá tới hết giờ.");

            await repository.DeleteAsync(token);
            logger.LogInformation("Discount feed Worker disconnected");
            return Result.Success("Đã ngắt kết nối máy thu ngân. Worker trên Cloudflare vẫn còn, có thể xoá trong trang quản trị Cloudflare.");
        }, cancellationToken);

    async Task<Result<string>> SaveAsync(DiscountFeedConnection? existing, string url, string writeToken, string? encryptedReadToken,
        string? accountId, string? scriptName)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var encryptedWrite = secretProtector.Protect(writeToken);
        DiscountFeedConnection connection;
        try
        {
            if (existing is null)
                connection = DiscountFeedConnection.Create(url, encryptedWrite, encryptedReadToken, accountId, scriptName, now);
            else
            {
                existing.Connect(url, encryptedWrite, encryptedReadToken, accountId, scriptName, now);
                connection = existing;
            }
        }
        catch (DomainException ex)
        {
            return Result.Failure<string>(ex.Message);
        }
        await repository.SaveAsync(connection, CancellationToken.None);
        return Result.Success("");
    }

    static string NewToken() => RandomNumberGenerator.GetHexString(64, lowercase: true);
}
