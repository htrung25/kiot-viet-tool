namespace KiotVietTool.Application.Interfaces;

public interface IOneTimeCodeService
{
    string GenerateOtp();
    IReadOnlyList<string> GenerateRecoveryCodes(int count);
    string HashRecoveryCode(string code);
    bool VerifyRecoveryCode(string code, string storedHash);
    bool OtpEquals(string expected, string actual);
}
