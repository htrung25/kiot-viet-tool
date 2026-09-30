using System.Text.RegularExpressions;

using KiotVietTool.Domain.Exceptions;

namespace KiotVietTool.Domain.Entities;

public sealed partial class KiotVietConnection
{
    public const int RetailerMaxLength = 100;
    public const int ClientIdMaxLength = 100;

    public int Id { get; private set; }
    public string Retailer { get; private set; } = "";
    public string ClientId { get; private set; } = "";
    public string EncryptedClientSecret { get; private set; } = "";
    public DateTime? LastSyncedAtUtc { get; private set; }
    public DateTime? ProductsSyncedFromUtc { get; private set; }

    private KiotVietConnection() { } // EF Core

    public static KiotVietConnection Create(string retailer, string clientId, string encryptedClientSecret)
    {
        var connection = new KiotVietConnection();
        connection.Update(retailer, clientId, encryptedClientSecret);
        return connection;
    }

    public void Update(string retailer, string clientId, string encryptedClientSecret)
    {
        retailer = NormalizeRetailer(retailer);
        EnsureValidRetailer(retailer);
        if (!retailer.Equals(Retailer, StringComparison.Ordinal) && Retailer.Length > 0)
            throw new DomainException("Dữ liệu đồng bộ và chương trình giảm giá của gian hàng cũ sẽ không dùng được với gian hàng mới. Hãy ngắt kết nối trước khi đổi gian hàng.");

        clientId = clientId.Trim();
        EnsureValidClientId(clientId);
        if (string.IsNullOrEmpty(encryptedClientSecret)) throw new DomainException("Client Secret không được để trống.");

        Retailer = retailer;
        ClientId = clientId;
        EncryptedClientSecret = encryptedClientSecret;
    }

    public void MarkSynced(DateTime startedAtUtc)
    {
        LastSyncedAtUtc = startedAtUtc;
        ProductsSyncedFromUtc = startedAtUtc;
    }

    public static string NormalizeRetailer(string retailer)
    {
        var value = retailer.Trim().ToLowerInvariant();
        var schemeEnd = value.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd >= 0) value = value[(schemeEnd + 3)..];
        var slash = value.IndexOf('/');
        if (slash >= 0) value = value[..slash];
        const string domain = ".kiotviet.vn";
        if (value.EndsWith(domain, StringComparison.Ordinal)) value = value[..^domain.Length];
        return value;
    }

    public static void EnsureValidRetailer(string normalizedRetailer)
    {
        if (normalizedRetailer.Length == 0) throw new DomainException("Retailer không được để trống.");
        if (normalizedRetailer.Length > RetailerMaxLength || !RetailerPattern().IsMatch(normalizedRetailer))
            throw new DomainException("Retailer không hợp lệ. Nhập tên gian hàng, ví dụ \"abc\" trong abc.kiotviet.vn.");
    }

    public static void EnsureValidClientId(string clientId)
    {
        if (clientId.Length == 0) throw new DomainException("Client ID không được để trống.");
        if (clientId.Length > ClientIdMaxLength) throw new DomainException("Client ID không hợp lệ.");
    }

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex RetailerPattern();
}
