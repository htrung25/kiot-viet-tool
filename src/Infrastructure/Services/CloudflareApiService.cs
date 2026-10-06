using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Exceptions;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Infrastructure.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KiotVietTool.Infrastructure.Services;

internal sealed class CloudflareApiService : ICloudflareApiService, IDisposable
{
    const string ScriptFileName = "discount-feed.js";
    const int KvPageSize = 100;

    const string NetworkMessage = "Không kết nối được Cloudflare (api.cloudflare.com). Kiểm tra Internet rồi thử lại.";
    const string PermissionMessage = "Cloudflare từ chối API Token (sai token, hết hạn hoặc thiếu quyền). Tạo token theo mẫu \"Edit Cloudflare Workers\".";
    const string NoSubdomainMessage = "Tài khoản Cloudflare chưa có tên miền workers.dev. Mở dash.cloudflare.com → Workers & Pages một lần để Cloudflare tạo, rồi bấm lại.";

    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    // Request bodies use Cloudflare's snake_case names exactly as written.
    static readonly JsonSerializerOptions RequestJson = new();

    readonly HttpClient _http;
    readonly CloudflareOptions _options;
    readonly ILogger<CloudflareApiService> _logger;

    public CloudflareApiService(IOptions<CloudflareOptions> options, ILogger<CloudflareApiService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) })
        {
            Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds),
        };
    }

    public async Task<IReadOnlyList<CloudflareAccountDto>> GetAccountsAsync(string apiToken, CancellationToken cancellationToken) =>
        [.. (await SendAsync<List<AccountJson>>(apiToken, HttpMethod.Get, "/accounts?per_page=50", null, cancellationToken))
            .Select(a => new CloudflareAccountDto(a.Id, string.IsNullOrWhiteSpace(a.Name) ? a.Id : a.Name))];

    public async Task<string> DeployDiscountFeedWorkerAsync(CloudflareWorkerDeployDto request, CancellationToken cancellationToken)
    {
        var account = "/accounts/" + Uri.EscapeDataString(request.AccountId);
        var subdomain = await GetSubdomainAsync(request.ApiToken, account, cancellationToken);
        var namespaceId = await EnsureKvNamespaceAsync(request.ApiToken, account, request.ScriptName, cancellationToken);
        await UploadScriptAsync(request, account, namespaceId, cancellationToken);
        await SendAsync<JsonElement>(request.ApiToken, HttpMethod.Post, $"{account}/workers/scripts/{request.ScriptName}/subdomain",
            JsonContent.Create(new { enabled = true, previews_enabled = false }, options: RequestJson), cancellationToken);
        _logger.LogInformation("Cloudflare Worker {Script} uploaded with KV namespace {Namespace}", request.ScriptName, namespaceId);
        return $"https://{request.ScriptName}.{subdomain}.workers.dev";
    }

    public void Dispose() => _http.Dispose();

    async Task<string> GetSubdomainAsync(string apiToken, string account, CancellationToken cancellationToken)
    {
        SubdomainJson subdomain;
        try
        {
            subdomain = await SendAsync<SubdomainJson>(apiToken, HttpMethod.Get, $"{account}/workers/subdomain", null, cancellationToken);
        }
        catch (CloudflareApiException ex) when (ex.StatusCode == 404)
        {
            throw new CloudflareApiException(NoSubdomainMessage, ex.StatusCode, ex);
        }
        return string.IsNullOrWhiteSpace(subdomain.Subdomain) ? throw new CloudflareApiException(NoSubdomainMessage) : subdomain.Subdomain;
    }

    async Task<string> EnsureKvNamespaceAsync(string apiToken, string account, string title, CancellationToken cancellationToken)
    {
        for (var page = 1; ; page++)
        {
            var namespaces = await SendAsync<List<NamespaceJson>>(apiToken, HttpMethod.Get,
                $"{account}/storage/kv/namespaces?per_page={KvPageSize}&page={page}", null, cancellationToken);
            if (namespaces.FirstOrDefault(n => n.Title == title) is { } existing) return existing.Id;
            if (namespaces.Count < KvPageSize) break;
        }

        var created = await SendAsync<NamespaceJson>(apiToken, HttpMethod.Post, $"{account}/storage/kv/namespaces",
            JsonContent.Create(new { title }, options: RequestJson), cancellationToken);
        _logger.LogInformation("Cloudflare KV namespace {Title} created", title);
        return created.Id;
    }

    async Task UploadScriptAsync(CloudflareWorkerDeployDto request, string account, string namespaceId, CancellationToken cancellationToken)
    {
        var metadata = new
        {
            main_module = ScriptFileName,
            compatibility_date = _options.CompatibilityDate,
            bindings = new object[]
            {
                new { type = "kv_namespace", name = "FEED", namespace_id = namespaceId },
                new { type = "secret_text", name = "WRITE_TOKEN", text = request.WriteToken },
                new { type = "secret_text", name = "READ_TOKEN", text = request.ReadToken },
            },
        };
        var metadataPart = JsonContent.Create(metadata, options: RequestJson);
        metadataPart.Headers.ContentDisposition = FormData("metadata");
        var scriptPart = new ByteArrayContent(ReadScript());
        scriptPart.Headers.ContentType = new MediaTypeHeaderValue("application/javascript+module");
        scriptPart.Headers.ContentDisposition = FormData(ScriptFileName, ScriptFileName);
        using var form = new MultipartFormDataContent { metadataPart, scriptPart };
        await SendAsync<JsonElement>(request.ApiToken, HttpMethod.Put, $"{account}/workers/scripts/{request.ScriptName}", form,
            cancellationToken);
    }

    // MultipartFormDataContent.Add(content, name) writes name=x unquoted plus filename*=; strict multipart parsers
    // reject that, so write the quoted form browsers, curl and wrangler send.
    static ContentDispositionHeaderValue FormData(string name, string? fileName = null) =>
        new("form-data") { Name = $"\"{name}\"", FileName = fileName is null ? null : $"\"{fileName}\"" };

    static byte[] ReadScript()
    {
        using var stream = typeof(CloudflareApiService).Assembly.GetManifestResourceStream(ScriptFileName)
            ?? throw new InvalidOperationException($"Embedded resource {ScriptFileName} is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    async Task<T> SendAsync<T>(string apiToken, HttpMethod method, string pathAndQuery, HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, _options.ApiBaseUrl.TrimEnd('/') + pathAndQuery) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        var path = pathAndQuery.Split('?')[0];
        var stopwatch = Stopwatch.StartNew();

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning("Cloudflare {Method} {Path} failed after {Elapsed} ms: {Error}", method, path, stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw new CloudflareApiException(NetworkMessage, null, ex);
        }

        using (response)
        {
            var status = (int)response.StatusCode;
            _logger.LogInformation("Cloudflare {Method} {Path} → {Status} in {Elapsed} ms", method, path, status, stopwatch.ElapsedMilliseconds);
            EnvelopeJson<T>? body;
            try
            {
                body = await response.Content.ReadFromJsonAsync<EnvelopeJson<T>>(Json, cancellationToken);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                throw new CloudflareApiException($"Cloudflare trả về dữ liệu không hợp lệ (mã {status}).", status, ex);
            }

            if (response.IsSuccessStatusCode && body is { Success: true, Result: { } result }) return result;
            throw ToException(status, method, path, body?.Errors);
        }
    }

    CloudflareApiException ToException(int status, HttpMethod method, string path, List<ErrorJson>? errors)
    {
        var first = errors?.FirstOrDefault();
        _logger.LogWarning("Cloudflare {Method} {Path} rejected with {Status}: {Code} {Message}", method, path, status, first?.Code,
            first?.Message);
        return status switch
        {
            401 or 403 => new CloudflareApiException(PermissionMessage, status),
            429 => new CloudflareApiException("Cloudflare đang giới hạn số lượt gọi. Thử lại sau ít phút.", status),
            >= 500 => new CloudflareApiException("Cloudflare đang gặp sự cố. Thử lại sau.", status),
            _ when first?.Code is 10000 or 9109 => new CloudflareApiException(PermissionMessage, status),
            _ => new CloudflareApiException($"Cloudflare từ chối yêu cầu: {first?.Message ?? $"mã {status}"}.", status),
        };
    }

    sealed record EnvelopeJson<T>(bool Success, T? Result, List<ErrorJson>? Errors);
    sealed record ErrorJson(int Code, string? Message);
    sealed record AccountJson(string Id, string? Name);
    sealed record SubdomainJson(string? Subdomain);
    sealed record NamespaceJson(string Id, string? Title);
}
