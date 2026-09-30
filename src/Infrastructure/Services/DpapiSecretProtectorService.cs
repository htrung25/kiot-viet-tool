using System.Security.Cryptography;
using System.Text;

using KiotVietTool.Application.Interfaces;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Infrastructure.Services;

internal sealed class DpapiSecretProtectorService(ILogger<DpapiSecretProtectorService> logger) : ISecretProtectorService
{
    const string DpapiPrefix = "dpapi:";
    const string DevPrefix = "dev:";
    static readonly byte[] Entropy = "KiotVietTool.ClientSecret"u8.ToArray();

    public string Protect(string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        if (OperatingSystem.IsWindows())
            return DpapiPrefix + Convert.ToBase64String(ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser));

        logger.LogWarning("DPAPI is Windows-only; the client secret is stored unencrypted (development machine)");
        return DevPrefix + Convert.ToBase64String(bytes);
    }

    public string? Unprotect(string protectedSecret)
    {
        try
        {
            if (protectedSecret.StartsWith(DpapiPrefix, StringComparison.Ordinal) && OperatingSystem.IsWindows())
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(
                    Convert.FromBase64String(protectedSecret[DpapiPrefix.Length..]), Entropy, DataProtectionScope.CurrentUser));
            if (protectedSecret.StartsWith(DevPrefix, StringComparison.Ordinal))
                return Encoding.UTF8.GetString(Convert.FromBase64String(protectedSecret[DevPrefix.Length..]));
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            logger.LogWarning("Stored client secret cannot be decrypted (other Windows user or machine?)");
        }
        return null;
    }
}
