using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Exceptions;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Infrastructure.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KiotVietTool.Infrastructure.Services;

internal sealed class DiscountFeedApiService : IDiscountFeedApiService, IDisposable
{
    const string FeedPath = "/v1/feed";
    const string NetworkMessage = "Không kết nối được Worker giảm giá. Kiểm tra Internet và địa chỉ Worker.";
    const string NotAFeedMessage = "Địa chỉ này không trả về danh sách giảm giá của KiotViet Tool. Kiểm tra lại địa chỉ Worker.";
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    readonly HttpClient _http;
    readonly ILogger<DiscountFeedApiService> _logger;

    public DiscountFeedApiService(IOptions<DiscountFeedOptions> options, ILogger<DiscountFeedApiService> logger)
    {
        _logger = logger;
        _http = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) })
        {
            Timeout = TimeSpan.FromSeconds(options.Value.RequestTimeoutSeconds),
        };
    }

    public async Task<DiscountFeedRemoteStateDto> GetStateAsync(DiscountFeedEndpointDto endpoint, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint.WorkerUrl + FeedPath);
        using var response = await SendAsync(request, endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode) throw ToException(response.StatusCode);

        var body = await ReadAsync<FeedJson>(response, cancellationToken) ?? throw new DiscountFeedException(NotAFeedMessage);
        if (body.Programs is null) throw new DiscountFeedException(NotAFeedMessage);
        var role = response.Headers.TryGetValues("X-Token-Role", out var values) ? values.FirstOrDefault() : null;
        return new DiscountFeedRemoteStateDto(Revision(response) ?? body.Revision ?? 0, body.InstanceId, body.ContentHash,
            body.GeneratedAtUtc, role);
    }

    public async Task<long> PublishAsync(DiscountFeedEndpointDto endpoint, DiscountFeedDto feed, long expectedRevision,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, endpoint.WorkerUrl + FeedPath)
        {
            Content = JsonContent.Create(feed, options: Json),
        };
        request.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{expectedRevision.ToString(CultureInfo.InvariantCulture)}\""));
        using var response = await SendAsync(request, endpoint, cancellationToken);

        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            var current = (await ReadAsync<ConflictJson>(response, cancellationToken))?.Current;
            throw new DiscountFeedConflictException(new DiscountFeedRemoteStateDto(
                current?.Revision ?? Revision(response) ?? 0, current?.InstanceId, current?.ContentHash, current?.GeneratedAtUtc));
        }
        if (!response.IsSuccessStatusCode) throw ToException(response.StatusCode);

        return (await ReadAsync<PutJson>(response, cancellationToken))?.Revision
            ?? throw new DiscountFeedException("Worker là phiên bản cũ, chưa có chống ghi đè giữa các máy. Vào Hệ thống → Máy thu ngân để cập nhật Worker.");
    }

    public void Dispose() => _http.Dispose();

    async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, DiscountFeedEndpointDto endpoint, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.Token);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await _http.SendAsync(request, cancellationToken);
            _logger.LogInformation("Discount feed {Method} {Host} → {Status} in {Elapsed} ms", request.Method, request.RequestUri?.Host,
                (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning("Discount feed {Method} {Host} failed after {Elapsed} ms: {Error}", request.Method, request.RequestUri?.Host,
                stopwatch.ElapsedMilliseconds, ex.Message);
            throw new DiscountFeedException(NetworkMessage, ex);
        }
    }

    static async Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) where T : class
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new DiscountFeedException(NotAFeedMessage, ex);
        }
    }

    static long? Revision(HttpResponseMessage response) =>
        response.Headers.ETag?.Tag is { } tag && long.TryParse(tag.Trim('"'), NumberStyles.None, CultureInfo.InvariantCulture, out var revision)
            ? revision
            : null;

    static DiscountFeedException ToException(HttpStatusCode status) => new((int)status switch
    {
        401 or 403 => "Worker từ chối mã (sai mã, hoặc mã đã được đổi trên Cloudflare).",
        404 => "Không tìm thấy Worker giảm giá ở địa chỉ này. Kiểm tra địa chỉ; Worker vừa cài có thể cần 1–2 phút mới chạy.",
        413 => "Danh sách giảm giá quá lớn.",
        >= 500 => $"Worker giảm giá đang lỗi (mã {(int)status}). Tool tự thử lại mỗi phút.",
        _ => $"Worker giảm giá trả lỗi {(int)status}.",
    });

    sealed record FeedJson(long? Revision, string? InstanceId, string? ContentHash, DateTime? GeneratedAtUtc, List<JsonElement>? Programs);
    sealed record ConflictJson(StateJson? Current);
    sealed record StateJson(long? Revision, string? InstanceId, string? ContentHash, DateTime? GeneratedAtUtc);
    sealed record PutJson(long? Revision);
}
