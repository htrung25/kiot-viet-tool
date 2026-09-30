using System.Globalization;
using System.Security.Cryptography;

using KiotVietTool.Application.Interfaces;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Infrastructure.Services;

internal sealed class Pbkdf2PasswordHasher(ILogger<Pbkdf2PasswordHasher> logger) : IPasswordHasher
{
    // Current format: "v1.<iterations>.<salt>.<hash>" (Base64), PBKDF2-HMAC-SHA256, OWASP 2023 iterations.
    // A new version gets its own prefix and sizes; stored iterations let the count rise without breaking old hashes.
    const string Version = "v1";
    const int Iterations = 600_000;
    const int SaltSize = 16;
    const int HashSize = 32;

    // Legacy format "pbkdf2-sha256$<iterations>$<salt>$<hash>" (same sizes): still verifies, always needs a rehash.
    const string LegacyScheme = "pbkdf2-sha256";

    const int MinIterations = 100_000;    // reject weakened hashes such as ".1."
    const int MaxIterations = 10_000_000; // cap CPU cost of a crafted hash

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Version}.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        if (!TryParse(passwordHash, out var stored))
        {
            // Never log the hash itself.
            logger.LogWarning("Stored password hash has an invalid format; sign-in rejected");
            return false;
        }

        // Output length is our constant, never taken from the stored value.
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, stored.Salt, stored.Iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(actual, stored.Hash);
    }

    public bool NeedsRehash(string passwordHash) =>
        TryParse(passwordHash, out var stored) && (stored.IsLegacy || stored.Iterations < Iterations);

    readonly record struct StoredHash(bool IsLegacy, int Iterations, byte[] Salt, byte[] Hash);

    /// <summary>Accepts only known formats: bounded iterations, exactly 16-byte salt and 32-byte hash.</summary>
    static bool TryParse(string passwordHash, out StoredHash stored)
    {
        stored = default;

        string[] parts;
        bool isLegacy;
        if (passwordHash.StartsWith(Version + ".", StringComparison.Ordinal))
        {
            parts = passwordHash.Split('.');
            isLegacy = false;
        }
        else if (passwordHash.StartsWith(LegacyScheme + "$", StringComparison.Ordinal))
        {
            parts = passwordHash.Split('$');
            isLegacy = true;
        }
        else return false;

        if (parts.Length != 4) return false;
        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
            || iterations is < MinIterations or > MaxIterations) return false;

        // Fixed-size buffers: decoding fails if the stored value is longer; the length check rejects shorter.
        var salt = new byte[SaltSize];
        var hash = new byte[HashSize];
        if (!Convert.TryFromBase64String(parts[2], salt, out var saltLength) || saltLength != SaltSize
            || !Convert.TryFromBase64String(parts[3], hash, out var hashLength) || hashLength != HashSize) return false;

        stored = new StoredHash(isLegacy, iterations, salt, hash);
        return true;
    }
}
