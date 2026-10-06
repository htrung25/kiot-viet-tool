using System.Text.RegularExpressions;

using KiotVietTool.Domain.Exceptions;

namespace KiotVietTool.Domain.Entities;

public sealed partial class DiscountFeedConnection
{
    public const int UrlMaxLength = 300;
    public const int ScriptNameMaxLength = 63;
    const string ScriptNamePrefix = "kvt-feed";

    public int Id { get; private set; }
    public string WorkerUrl { get; private set; } = "";
    public string EncryptedWriteToken { get; private set; } = "";
    public string? EncryptedReadToken { get; private set; }
    public string? CloudflareAccountId { get; private set; }
    public string? ScriptName { get; private set; }
    public string InstanceId { get; private set; } = "";
    public long LastRevision { get; private set; }
    public DateTime ConnectedAtUtc { get; private set; }

    private DiscountFeedConnection() { } // EF Core

    /// <summary>True when the tool deployed the Worker (and can update it again with a Cloudflare API token).</summary>
    public bool IsManaged => ScriptName is not null;

    public static DiscountFeedConnection Create(string workerUrl, string encryptedWriteToken, string? encryptedReadToken,
        string? cloudflareAccountId, string? scriptName, DateTime nowUtc)
    {
        var connection = new DiscountFeedConnection();
        connection.Connect(workerUrl, encryptedWriteToken, encryptedReadToken, cloudflareAccountId, scriptName, nowUtc);
        return connection;
    }

    public void Connect(string workerUrl, string encryptedWriteToken, string? encryptedReadToken,
        string? cloudflareAccountId, string? scriptName, DateTime nowUtc)
    {
        workerUrl = NormalizeUrl(workerUrl);
        if (string.IsNullOrEmpty(encryptedWriteToken)) throw new DomainException("Mã ghi không được để trống.");
        if (scriptName is not null) EnsureValidScriptName(scriptName);

        if (!workerUrl.Equals(WorkerUrl, StringComparison.Ordinal))
        {
            InstanceId = Guid.NewGuid().ToString("N");
            LastRevision = 0;
        }
        WorkerUrl = workerUrl;
        EncryptedWriteToken = encryptedWriteToken;
        EncryptedReadToken = string.IsNullOrEmpty(encryptedReadToken) ? null : encryptedReadToken;
        CloudflareAccountId = string.IsNullOrWhiteSpace(cloudflareAccountId) ? null : cloudflareAccountId.Trim();
        ScriptName = scriptName;
        ConnectedAtUtc = nowUtc;
    }

    public static string NormalizeUrl(string url)
    {
        var value = (url ?? "").Trim().TrimEnd('/');
        if (value.Length == 0) throw new DomainException("Nhập địa chỉ Worker.");
        if (value.Length > UrlMaxLength
            || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !(uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
            || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0)
            throw new DomainException("Địa chỉ Worker phải có dạng https://ten-worker.ten-ban.workers.dev");
        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    public static void EnsureValidScriptName(string scriptName)
    {
        if (scriptName.Length == 0) throw new DomainException("Nhập tên Worker.");
        if (scriptName.Length > ScriptNameMaxLength || !ScriptNamePattern().IsMatch(scriptName))
            throw new DomainException($"Tên Worker chỉ gồm chữ thường không dấu, số và dấu gạch ngang, tối đa {ScriptNameMaxLength} ký tự.");
    }

    public static string SuggestScriptName(string? retailer)
    {
        var suffix = string.IsNullOrWhiteSpace(retailer) ? "" : "-" + retailer.Trim().ToLowerInvariant();
        var name = (ScriptNamePrefix + suffix)[..Math.Min(ScriptNameMaxLength, ScriptNamePrefix.Length + suffix.Length)];
        name = name.TrimEnd('-');
        return ScriptNamePattern().IsMatch(name) ? name : ScriptNamePrefix;
    }

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]*[a-z0-9])?$")]
    private static partial Regex ScriptNamePattern();
}
