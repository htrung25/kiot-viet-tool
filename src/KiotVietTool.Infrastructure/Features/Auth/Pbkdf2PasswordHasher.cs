using System.Security.Cryptography;

using KiotVietTool.Application.Features.Auth;

namespace KiotVietTool.Infrastructure.Features.Auth;

/// <summary>
/// PBKDF2-HMAC-SHA256 (OWASP 2023: 600k iterations). Format: <c>pbkdf2-sha256$iterations$salt$hash</c> (Base64),
/// so iterations can be raised later without breaking existing hashes.
/// </summary>
internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    const string Scheme = "pbkdf2-sha256";
    const int Iterations = 600_000;
    const int SaltSize = 16;
    const int HashSize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Scheme}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('$');
        if (parts.Length != 4 || parts[0] != Scheme || !int.TryParse(parts[1], out var iterations) || iterations <= 0)
            return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }
}
