using System.Diagnostics;
using System.Net;
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

internal sealed class TelegramApiService : ITelegramApiService, IDisposable
{
    const string NetworkMessage = "Không kết nối được Telegram (api.telegram.org). Kiểm tra Internet, hoặc mạng đang chặn Telegram (cần cấu hình Telegram:ProxyUrl).";
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    readonly HttpClient _http;
    readonly TelegramOptions _options;
    readonly ILogger<TelegramApiService> _logger;

    public TelegramApiService(IOptions<TelegramOptions> options, ILogger<TelegramApiService> logger)
    {
        _options = options.Value;
        _logger = logger;
        var handler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) };
        if (!string.IsNullOrWhiteSpace(_options.ProxyUrl))
        {
            handler.Proxy = new WebProxy(_options.ProxyUrl);
            handler.UseProxy = true;
        }
        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds) };
    }

    public async Task<string> GetBotUsernameAsync(string botToken, CancellationToken cancellationToken) =>
        (await CallAsync<UserJson>(botToken, "getMe", null, cancellationToken)).Username ?? "";

    public async Task<TelegramChatDto?> FindLatestStartChatAsync(string botToken, CancellationToken cancellationToken)
    {
        var updates = await CallAsync<List<UpdateJson>>(botToken, "getUpdates", new { allowed_updates = new[] { "message" } }, cancellationToken);
        var message = updates
            .Select(u => u.Message)
            .Where(m => m is { Chat.Type: "private" } && (m.Text ?? "").Trim().StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m!.Date)
            .FirstOrDefault();
        if (message?.Chat is not { } chat) return null;

        var name = string.Join(" ", new[] { chat.FirstName, chat.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var title = chat.Username is { Length: > 0 } username ? $"{name} (@{username})".Trim() : name;
        return new TelegramChatDto(chat.Id, string.IsNullOrWhiteSpace(title) ? chat.Id.ToString() : title);
    }

    public Task SendMessageAsync(string botToken, long chatId, string text, CancellationToken cancellationToken) =>
        CallAsync<JsonElement>(botToken, "sendMessage", new { chat_id = chatId, text }, cancellationToken);

    public void Dispose() => _http.Dispose();

    async Task<T> CallAsync<T>(string botToken, string method, object? body, CancellationToken cancellationToken)
    {
        var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/bot{botToken}/{method}";
        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = body is null
                ? await _http.GetAsync(url, cancellationToken)
                : await _http.PostAsJsonAsync(url, body, Json, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning("Telegram {Method} failed after {Elapsed} ms: {Error}", method, stopwatch.ElapsedMilliseconds, ex.GetType().Name);
            throw new TelegramApiException(NetworkMessage);
        }

        using (response)
        {
            _logger.LogInformation("Telegram {Method} → {Status} in {Elapsed} ms", method, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
            ResponseJson<T>? payload;
            try
            {
                payload = await response.Content.ReadFromJsonAsync<ResponseJson<T>>(Json, cancellationToken);
            }
            catch (JsonException)
            {
                throw new TelegramApiException($"Telegram trả về dữ liệu không hợp lệ (mã {(int)response.StatusCode}).");
            }

            if (payload is { Ok: true, Result: { } result }) return result;
            throw ToException(payload?.ErrorCode ?? (int)response.StatusCode, payload?.Description ?? "", payload?.Parameters?.RetryAfter);
        }
    }

    TelegramApiException ToException(int code, string description, int? retryAfter)
    {
        _logger.LogWarning("Telegram error {Code}: {Description}", code, description);
        return code switch
        {
            401 or 404 => new TelegramApiException("Bot Token không đúng hoặc đã bị thu hồi. Kiểm tra lại token từ @BotFather."),
            409 => new TelegramApiException("Bot đang dùng webhook nên tool không đọc được tin nhắn. Tắt webhook của bot (deleteWebhook) hoặc tạo bot mới."),
            403 => new TelegramApiException("Bot không gửi được tin: bạn đã chặn bot hoặc chưa bấm Bắt đầu (/start). Mở bot trên Telegram và bấm /start."),
            400 when description.Contains("chat not found", StringComparison.OrdinalIgnoreCase) =>
                new TelegramApiException("Không tìm thấy cuộc trò chuyện. Mở bot trên Telegram và bấm /start."),
            429 => new TelegramApiException($"Telegram đang giới hạn số lượt gửi. Thử lại sau {retryAfter ?? 30} giây."),
            _ => new TelegramApiException($"Telegram từ chối yêu cầu (mã {code}). Chi tiết đã được ghi vào file log."),
        };
    }

    sealed record ResponseJson<T>(bool Ok, T? Result, [property: JsonPropertyName("error_code")] int? ErrorCode,
        string? Description, ParametersJson? Parameters);
    sealed record ParametersJson([property: JsonPropertyName("retry_after")] int? RetryAfter);
    sealed record UserJson(string? Username);
    sealed record UpdateJson(MessageJson? Message);
    sealed record MessageJson(ChatJson? Chat, string? Text, long Date);
    sealed record ChatJson(long Id, string? Type, [property: JsonPropertyName("first_name")] string? FirstName,
        [property: JsonPropertyName("last_name")] string? LastName, string? Username);
}
