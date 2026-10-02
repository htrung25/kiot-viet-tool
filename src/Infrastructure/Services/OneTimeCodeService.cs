using System.Security.Cryptography;
using System.Text;

using KiotVietTool.Application.Interfaces;

namespace KiotVietTool.Infrastructure.Services;

internal sealed class OneTimeCodeService : IOneTimeCodeService
{
    const string RecoveryAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    const int RecoveryLength = 16;
    const string HashVersion = "s1";

    public string GenerateOtp() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public IReadOnlyList<string> GenerateRecoveryCodes(int count) =>
        [.. Enumerable.Range(0, count).Select(_ =>
        {
            var chars = RandomNumberGenerator.GetString(RecoveryAlphabet, RecoveryLength);
            return string.Join("-", Enumerable.Range(0, RecoveryLength / 4).Select(i => chars.Substring(i * 4, 4)));
        })];

    public string HashRecoveryCode(string code)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return $"{HashVersion}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(Hash(salt, Normalize(code)))}";
    }

    public bool VerifyRecoveryCode(string code, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3 || parts[0] != HashVersion) return false;
        try
        {
            var expected = Convert.FromBase64String(parts[2]);
            return CryptographicOperations.FixedTimeEquals(Hash(Convert.FromBase64String(parts[1]), Normalize(code)), expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public bool OtpEquals(string expected, string actual) =>
        actual.Length == expected.Length
        && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(actual));

    static string Normalize(string code) =>
        new([.. code.ToUpperInvariant().Where(c => c is not ('-' or ' '))]);

    static byte[] Hash(byte[] salt, string code) => SHA256.HashData([.. salt, .. Encoding.UTF8.GetBytes(code)]);
}
