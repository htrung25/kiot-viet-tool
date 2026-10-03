using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Exceptions;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Enums;
using KiotVietTool.Infrastructure.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KiotVietTool.Infrastructure.Services;

internal sealed class KiotVietApiService : IKiotVietApiService, IDisposable
{
    const int PageSize = 100;
    static readonly TimeSpan TokenRefreshMargin = TimeSpan.FromSeconds(60);
    static readonly TimeSpan MaxRetryAfter = TimeSpan.FromSeconds(60);
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    const string InvalidCredentialsMessage = "Client ID hoặc Client Secret không đúng.";
    const string NetworkMessage = "Không kết nối được KiotViet. Kiểm tra Internet rồi thử lại.";
    const string RateLimitedMessage = "KiotViet đang giới hạn số lượt gọi. Thử lại sau ít phút.";
    const string ServerErrorMessage = "KiotViet đang gặp sự cố. Thử lại sau.";

    readonly HttpClient _http = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) })
    {
        Timeout = Timeout.InfiniteTimeSpan,
    };
    readonly KiotVietOptions _options;
    readonly TimeProvider _timeProvider;
    readonly IServerClockService _clock;
    readonly ILogger<KiotVietApiService> _logger;
    readonly Queue<DateTime> _recentGets = new();
    readonly SemaphoreSlim _getLock = new(1, 1);
    readonly SemaphoreSlim _writeLock = new(1, 1);
    DateTimeOffset _lastWrite = DateTimeOffset.MinValue;
    readonly SemaphoreSlim _tokenLock = new(1, 1);
    CachedToken? _token;
    bool _missingTypeLogged;

    public KiotVietApiService(IOptions<KiotVietOptions> options, TimeProvider timeProvider, IServerClockService clock,
        ILogger<KiotVietApiService> logger)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        _clock = clock;
        _logger = logger;
    }

    public async Task SyncClockAsync(CancellationToken cancellationToken)
    {
        if (!_clock.IsStale) return;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, _options.ApiBaseUrl);
            using var response = await _http.SendAsync(request, timeout.Token);
            if (response.Headers.Date is { } date) _clock.Observe(date);
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogInformation("Could not read KiotViet server time: {Error}", ex.Message);
        }
    }

    public async Task<int> CountProductsAsync(KiotVietCredentialsDto credentials, CancellationToken cancellationToken) =>
        (await GetAsync<PageJson<ProductJson>>(credentials, "/products?pageSize=1", cancellationToken)).Total;

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(KiotVietCredentialsDto credentials, CancellationToken cancellationToken) =>
        [.. (await GetAllAsync<CategoryJson>(credentials, "/categories?hierachicalData=false", cancellationToken))
            .Select(c => Category.Create(c.CategoryId, c.CategoryName ?? "", c.ParentId))];

    public async Task<KiotVietPageDto<Product>> GetProductsPageAsync(KiotVietCredentialsDto credentials, DateTime? modifiedFromUtc,
        int currentItem, CancellationToken cancellationToken)
    {
        var query = $"/products?pageSize={PageSize}&currentItem={currentItem}&includeRemoveIds=true";
        if (modifiedFromUtc is { } from)
            query += "&lastModifiedFrom=" + Uri.EscapeDataString(
                VietnamTime.FromUtc(from).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));

        var page = await GetAsync<PageJson<ProductJson>>(credentials, query, cancellationToken);
        return new KiotVietPageDto<Product>([.. (page.Data ?? []).Select(ToProduct)], page.Total, page.RemoveId ?? []);
    }

    public async Task<IReadOnlyList<PriceBook>> GetPriceBooksAsync(KiotVietCredentialsDto credentials, CancellationToken cancellationToken) =>
        [.. (await GetAllAsync<PriceBookJson>(credentials,
                "/pricebooks?includePriceBookBranch=true&includePriceBookCustomerGroups=true&includePriceBookUsers=true",
                cancellationToken))
            .Select(b => PriceBook.Create(b.Id, b.Name ?? "", b.IsActive ?? false, b.IsGlobal ?? false,
                ToUtc(b.StartDate), ToUtc(b.EndDate), b.ForAllCusGroup ?? true, b.ForAllUser ?? true,
                (b.PriceBookBranches ?? []).Select(x => x.BranchId),
                (b.PriceBookCustomerGroups ?? []).Select(x => x.CustomerGroupId),
                (b.PriceBookUsers ?? []).Select(x => x.UserId)))];

    public async Task<IReadOnlyList<PriceBookItem>> GetPriceBookItemsAsync(KiotVietCredentialsDto credentials, long priceBookId,
        CancellationToken cancellationToken) =>
        [.. (await GetAllAsync<PriceBookItemJson>(credentials, $"/pricebooks/{priceBookId}?", cancellationToken))
            .Select(i => PriceBookItem.Create(priceBookId, i.ProductId, i.Price))];

    public int PriceBatchSize => _options.PriceUpdateBatchSize;

    public async Task<IReadOnlyDictionary<long, decimal>> GetBasePricesAsync(KiotVietCredentialsDto credentials, CancellationToken cancellationToken)
    {
        var prices = new Dictionary<long, decimal>();
        while (true)
        {
            var page = await GetAsync<PageJson<ProductJson>>(credentials,
                $"/products?pageSize={PageSize}&currentItem={prices.Count}", cancellationToken);
            var data = page.Data ?? [];
            foreach (var product in data) prices[product.Id] = product.BasePrice ?? 0;
            if (data.Count == 0 || prices.Count >= page.Total) return prices;
        }
    }

    public Task UpdateBasePricesAsync(KiotVietCredentialsDto credentials, IReadOnlyList<ProductPriceUpdateDto> prices,
        CancellationToken cancellationToken) =>
        SendAuthorizedAsync<JsonElement>(credentials, HttpMethod.Put, "/listupdatedproducts",
            new { listProducts = prices.Select(p => new { id = p.ProductId, basePrice = p.BasePrice }) }, cancellationToken);

    public Task UpdateBasePriceAsync(KiotVietCredentialsDto credentials, ProductPriceUpdateDto price, CancellationToken cancellationToken) =>
        SendAuthorizedAsync<JsonElement>(credentials, HttpMethod.Put, $"/products/{price.ProductId}",
            new { basePrice = price.BasePrice }, cancellationToken);

    public void Dispose()
    {
        _http.Dispose();
        _getLock.Dispose();
        _writeLock.Dispose();
        _tokenLock.Dispose();
    }

    Product ToProduct(ProductJson p)
    {
        var type = p.Type ?? p.ProductType;
        if (type is null && !_missingTypeLogged)
        {
            _missingTypeLogged = true;
            _logger.LogWarning("KiotViet product list has no type/productType field; products default to goods");
        }
        return Product.Create(p.Id, p.Code ?? "", p.Name ?? "", p.FullName, p.CategoryId, p.BasePrice ?? 0, p.Unit,
            p.MasterUnitId, p.ConversionValue, p.MasterProductId,
            type is 1 or 2 or 3 ? (ProductEnum)type.Value : ProductEnum.Goods,
            p.IsActive ?? true, p.AllowsSale ?? true, ToUtc(p.ModifiedDate));
    }

    static DateTime? ToUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Unspecified } v => VietnamTime.ToUtc(v),
        { } v => v.ToUniversalTime(),
    };

    async Task<List<T>> GetAllAsync<T>(KiotVietCredentialsDto credentials, string pathAndQuery, CancellationToken cancellationToken)
    {
        var separator = pathAndQuery.EndsWith('?') ? "" : "&";
        var items = new List<T>();
        while (true)
        {
            var page = await GetAsync<PageJson<T>>(credentials,
                $"{pathAndQuery}{separator}pageSize={PageSize}&currentItem={items.Count}", cancellationToken);
            var data = page.Data ?? [];
            items.AddRange(data);
            if (data.Count == 0 || items.Count >= page.Total) return items;
        }
    }

    Task<T> GetAsync<T>(KiotVietCredentialsDto credentials, string pathAndQuery, CancellationToken cancellationToken) =>
        SendAuthorizedAsync<T>(credentials, HttpMethod.Get, pathAndQuery, null, cancellationToken);

    async Task<T> SendAuthorizedAsync<T>(KiotVietCredentialsDto credentials, HttpMethod method, string pathAndQuery, object? body,
        CancellationToken cancellationToken)
    {
        var url = _options.ApiBaseUrl.TrimEnd('/') + pathAndQuery;
        var isGet = method == HttpMethod.Get;
        var forceNewToken = false;
        while (true)
        {
            var token = await GetTokenAsync(credentials, forceNewToken, cancellationToken);
            if (!isGet) await WaitForWriteSlotAsync(cancellationToken);
            using var response = await SendAsync(() =>
            {
                var request = new HttpRequestMessage(method, url);
                request.Headers.Add("Retailer", credentials.Retailer);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                if (body is not null) request.Content = JsonContent.Create(body, options: Json);
                return request;
            }, isGet, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized && !forceNewToken)
            {
                forceNewToken = true;
                continue;
            }
            if (!response.IsSuccessStatusCode)
                throw await ToApiExceptionAsync(response, credentials.Retailer, cancellationToken);

            try
            {
                return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
                    ?? throw new KiotVietApiException("KiotViet trả về dữ liệu rỗng.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Unexpected KiotViet response for {Url}", url);
                throw new KiotVietApiException("KiotViet trả về dữ liệu không đúng định dạng. Chi tiết đã được ghi vào file log.", ex);
            }
        }
    }

    async Task<string> GetTokenAsync(KiotVietCredentialsDto credentials, bool forceNew, CancellationToken cancellationToken)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (!forceNew && _token is { } cached && cached.Matches(credentials) && cached.ExpiresAtUtc - TokenRefreshMargin > now)
                return cached.AccessToken;

            using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["scopes"] = "PublicApi.Access",
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = credentials.ClientId,
                    ["client_secret"] = credentials.ClientSecret,
                }),
            }, isGet: false, cancellationToken);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _token = null;
                throw new KiotVietApiException(InvalidCredentialsMessage);
            }
            if (!response.IsSuccessStatusCode)
                throw await ToApiExceptionAsync(response, credentials.Retailer, cancellationToken);

            var body = await response.Content.ReadFromJsonAsync<TokenJson>(Json, cancellationToken);
            if (string.IsNullOrEmpty(body?.AccessToken)) throw new KiotVietApiException(InvalidCredentialsMessage);

            _token = new CachedToken(credentials.ClientId, credentials.ClientSecret, body.AccessToken,
                now.AddSeconds(body.ExpiresIn > 0 ? body.ExpiresIn : 3600));
            return body.AccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest, bool isGet, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            if (isGet) await WaitForGetSlotAsync(cancellationToken);

            using var request = createRequest();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));
            var stopwatch = Stopwatch.StartNew();
            string failure;
            TimeSpan? retryAfter = null;
            try
            {
                var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token);
                if (response.Headers.Date is { } serverTime) _clock.Observe(serverTime);
                _logger.LogInformation("KiotViet {Method} {Path} → {Status} in {Elapsed} ms",
                    request.Method, request.RequestUri?.AbsolutePath, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

                var isTransient = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
                if (!isTransient || attempt >= _options.MaxRetries) return response;

                failure = response.StatusCode == HttpStatusCode.TooManyRequests ? RateLimitedMessage : ServerErrorMessage;
                retryAfter = response.Headers.RetryAfter?.Delta;
                response.Dispose();
            }
            catch (Exception ex) when (ex is HttpRequestException || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                _logger.LogWarning("KiotViet {Method} {Path} failed after {Elapsed} ms: {Error}",
                    request.Method, request.RequestUri?.AbsolutePath, stopwatch.ElapsedMilliseconds, ex.Message);
                if (attempt >= _options.MaxRetries) throw new KiotVietApiException(NetworkMessage, ex);
                failure = NetworkMessage;
            }

            var delay = retryAfter is { } after && after > TimeSpan.Zero
                ? (after < MaxRetryAfter ? after : MaxRetryAfter)
                : TimeSpan.FromSeconds(1 << attempt);
            _logger.LogInformation("Retrying KiotViet request in {Delay} ({Reason})", delay, failure);
            await Task.Delay(delay, _timeProvider, cancellationToken);
        }
    }

    async Task WaitForWriteSlotAsync(CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var wait = _lastWrite + TimeSpan.FromMilliseconds(_options.MinWriteIntervalMs) - _timeProvider.GetUtcNow();
            if (wait > TimeSpan.Zero) await Task.Delay(wait, _timeProvider, cancellationToken);
            _lastWrite = _timeProvider.GetUtcNow();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    async Task WaitForGetSlotAsync(CancellationToken cancellationToken)
    {
        await _getLock.WaitAsync(cancellationToken);
        try
        {
            var window = TimeSpan.FromHours(1);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            while (_recentGets.TryPeek(out var oldest) && oldest <= now - window) _recentGets.Dequeue();
            if (_recentGets.Count >= _options.MaxGetRequestsPerHour)
            {
                var wait = _recentGets.Peek() + window - now;
                _logger.LogWarning("KiotViet GET limit of {Limit}/hour reached; waiting {Wait}", _options.MaxGetRequestsPerHour, wait);
                await Task.Delay(wait, _timeProvider, cancellationToken);
                _recentGets.Dequeue();
            }
            _recentGets.Enqueue(_timeProvider.GetUtcNow().UtcDateTime);
        }
        finally
        {
            _getLock.Release();
        }
    }

    async Task<KiotVietApiException> ToApiExceptionAsync(HttpResponseMessage response, string retailer, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Length > 500) body = body[..500];
        _logger.LogWarning("KiotViet request {Path} rejected with {Status}: {Body}",
            response.RequestMessage?.RequestUri?.AbsolutePath, status, body);

        return response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => new KiotVietApiException(RateLimitedMessage),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new KiotVietApiException(
                $"Không truy cập được gian hàng \"{retailer}\". Kiểm tra lại tên Retailer và gian hàng đã bật Thiết lập kết nối API (Thiết lập cửa hàng → Thiết lập kết nối API)."),
            _ when status >= 500 => new KiotVietApiException(ServerErrorMessage),
            _ when body.Contains("retailer", StringComparison.OrdinalIgnoreCase) => new KiotVietApiException(
                "Không tìm thấy gian hàng. Kiểm tra lại tên Retailer.", statusCode: status),
            _ => new KiotVietApiException($"KiotViet từ chối yêu cầu (mã {status}). Chi tiết đã được ghi vào file log.",
                statusCode: status),
        };
    }

    sealed record CachedToken(string ClientId, string ClientSecret, string AccessToken, DateTime ExpiresAtUtc)
    {
        public bool Matches(KiotVietCredentialsDto credentials) =>
            ClientId == credentials.ClientId && ClientSecret == credentials.ClientSecret;
    }

    sealed record TokenJson(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string? AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn);

    sealed record PageJson<T>(int Total, List<T>? Data, List<long>? RemoveId);

    sealed record CategoryJson(int CategoryId, string? CategoryName, int? ParentId);

    sealed record ProductJson(
        long Id, string? Code, string? Name, string? FullName, int? CategoryId, decimal? BasePrice, string? Unit,
        long? MasterUnitId, double? ConversionValue, long? MasterProductId, int? Type, int? ProductType,
        bool? IsActive, bool? AllowsSale, DateTime? ModifiedDate);

    sealed record PriceBookJson(
        long Id, string? Name, bool? IsActive, bool? IsGlobal, DateTime? StartDate, DateTime? EndDate,
        bool? ForAllCusGroup, bool? ForAllUser, List<BranchJson>? PriceBookBranches,
        List<CustomerGroupJson>? PriceBookCustomerGroups, List<UserJson>? PriceBookUsers);

    sealed record BranchJson(long BranchId);
    sealed record CustomerGroupJson(long CustomerGroupId);
    sealed record UserJson(long UserId);
    sealed record PriceBookItemJson(long ProductId, decimal Price);
}
