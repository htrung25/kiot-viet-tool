using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Exceptions;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Exceptions;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class KiotVietConnectionService(
    IKiotVietConnectionRepository repository,
    IKiotVietApiService api,
    ISecretProtectorService secretProtector,
    ICatalogSyncService sync,
    IDiscountProgramRepository programs,
    ILogger<KiotVietConnectionService> logger) : IKiotVietConnectionService
{
    public async Task<KiotVietConnectionDto?> GetAsync(CancellationToken cancellationToken = default) =>
        await repository.GetAsync(cancellationToken) is { } connection
            ? new KiotVietConnectionDto(connection.Retailer, connection.ClientId, connection.LastSyncedAtUtc)
            : null;

    public async Task<Result<int>> TestAsync(SaveKiotVietConnectionDto request, CancellationToken cancellationToken = default)
    {
        var credentials = await ResolveCredentialsAsync(request, cancellationToken);
        if (!credentials.IsSuccess) return Result.Failure<int>(credentials.Error!);

        try
        {
            var count = await api.CountProductsAsync(credentials.Value!, cancellationToken);
            logger.LogInformation("KiotViet connection test succeeded for retailer {Retailer}", credentials.Value!.Retailer);
            return Result.Success(count);
        }
        catch (KiotVietApiException ex)
        {
            return Result.Failure<int>(ex.Message);
        }
    }

    public async Task<Result> SaveAsync(SaveKiotVietConnectionDto request, CancellationToken cancellationToken = default)
    {
        var test = await TestAsync(request, cancellationToken);
        if (!test.IsSuccess) return test;

        var existing = await repository.GetAsync(cancellationToken);
        var newSecret = request.ClientSecret?.Trim();
        var encryptedSecret = string.IsNullOrEmpty(newSecret)
            ? existing?.EncryptedClientSecret ?? ""
            : secretProtector.Protect(newSecret);

        try
        {
            if (existing is null) existing = KiotVietConnection.Create(request.Retailer, request.ClientId, encryptedSecret);
            else existing.Update(request.Retailer, request.ClientId, encryptedSecret);
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        await repository.SaveAsync(existing, cancellationToken);
        logger.LogInformation("KiotViet connection saved for retailer {Retailer}", existing.Retailer);
        return Result.Success();
    }

    public async Task<Result> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (sync.IsRunning) return Result.Failure("Đang đồng bộ dữ liệu. Hãy chờ đồng bộ xong rồi ngắt kết nối.");

        var active = (await programs.GetAllAsync(cancellationToken))
            .Where(p => p.HoldsKiotVietPrices)
            .Select(p => p.Name).ToList();
        if (active.Count > 0)
            return Result.Failure($"Còn chương trình đang giữ giá giảm trên KiotViet: {string.Join(", ", active)}. Hãy dừng các chương trình này (trả giá gốc) trước khi ngắt kết nối.");

        await repository.DeleteWithSyncedDataAsync(cancellationToken);
        logger.LogInformation("KiotViet connection removed with its synced data");
        return Result.Success();
    }

    async Task<Result<KiotVietCredentialsDto>> ResolveCredentialsAsync(SaveKiotVietConnectionDto request, CancellationToken cancellationToken)
    {
        var retailer = KiotVietConnection.NormalizeRetailer(request.Retailer);
        var clientId = request.ClientId.Trim();
        try
        {
            KiotVietConnection.EnsureValidRetailer(retailer);
            KiotVietConnection.EnsureValidClientId(clientId);
        }
        catch (DomainException ex)
        {
            return Result.Failure<KiotVietCredentialsDto>(ex.Message);
        }

        var secret = request.ClientSecret?.Trim();
        if (string.IsNullOrEmpty(secret))
        {
            var existing = await repository.GetAsync(cancellationToken);
            secret = existing is null ? null : secretProtector.Unprotect(existing.EncryptedClientSecret);
            if (string.IsNullOrEmpty(secret)) return Result.Failure<KiotVietCredentialsDto>("Nhập Client Secret.");
        }

        return Result.Success(new KiotVietCredentialsDto(retailer, clientId, secret));
    }
}
