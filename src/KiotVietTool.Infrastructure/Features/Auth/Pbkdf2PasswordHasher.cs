using System.Globalization;
using System.Security.Cryptography;

using KiotVietTool.Application.Features.Auth;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Infrastructure.Features.Auth;

/// <summary>
/// PBKDF2-HMAC-SHA256 (OWASP 2023: 600k iterations). Format: <c>pbkdf2-sha256$iterations$salt$hash</c> (Base64),
/// so iterations can be raised later without breaking existing hashes.
/// </summary>
internal sealed class Pbkdf2PasswordHasher(ILogger<Pbkdf2PasswordHasher> logger) : IPasswordHasher
{
    const string Scheme = "pbkdf2-sha256";
    const int Iterations = 600_000;
    const int SaltSize = 16;
    const int HashSize = 32;
    const int MinIterations = 100_000;    // reject weakened hashes such as "$1$"
    const int MaxIterations = 10_000_000; // cap CPU cost of a crafted hash

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Scheme}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        if (!TryParse(passwordHash, out var iterations, out var salt, out var expected))
        {
            // Never log the hash itself.
            logger.LogWarning("Stored password hash has an invalid format; sign-in rejected");
            return false;
        }

        // Output length is our constant, never taken from the stored value.
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    /// <summary>Accepts only our exact format: known scheme, bounded iterations, 16-byte salt, 32-byte hash.</summary>
    static bool TryParse(string passwordHash, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = new byte[SaltSize];
        hash = new byte[HashSize];

        var parts = passwordHash.Split('$');
        if (parts.Length != 4 || parts[0] != Scheme) return false;
        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations)
            || iterations is < MinIterations or > MaxIterations) return false;

        // Fixed-size buffers: decoding fails if the stored value is longer; the length check rejects shorter.
        return Convert.TryFromBase64String(parts[2], salt, out var saltLength) && saltLength == SaltSize
            && Convert.TryFromBase64String(parts[3], hash, out var hashLength) && hashLength == HashSize;
    }
}
