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

internal sealed class DiscountFeedApiService(IOptions<DiscountFeedOptions> options, ILogger<DiscountFeedApiService> logger)
    : IDiscountFeedApiService, IDisposable
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    readonly DiscountFeedOptions _options = options.Value;
    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(options.Value.RequestTimeoutSeconds) };

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Url);

    public async Task PublishAsync(DiscountFeedDto feed, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, _options.Url.TrimEnd('/') + "/v1/feed")
        {
            Content = JsonContent.Create(feed, options: Json),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.WriteToken);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new DiscountFeedException("Không kết nối được máy chủ giảm giá. Kiểm tra Internet.", ex);
        }

        using (response)
        {
            logger.LogInformation("Discount feed PUT → {Status}", (int)response.StatusCode);
            if (response.IsSuccessStatusCode) return;
            throw new DiscountFeedException((int)response.StatusCode switch
            {
                401 or 403 => "Máy chủ giảm giá từ chối mã ghi (DiscountFeed:WriteToken).",
                413 => "Danh sách giảm giá quá lớn.",
                _ => $"Máy chủ giảm giá trả lỗi {(int)response.StatusCode}.",
            });
        }
    }

    public void Dispose() => _http.Dispose();
}
