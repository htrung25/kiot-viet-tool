using KiotVietTool.Domain.Exceptions;

namespace KiotVietTool.Domain.Entities;

public sealed class UserAccount
{
    public const int UsernameMaxLength = 50;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
    public const int MaxFailedOtpAttempts = 10;
    public static readonly TimeSpan OtpLockDuration = TimeSpan.FromMinutes(15);

    public int Id { get; private set; }
    public string Username { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public bool MustChangePassword { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public List<string> RecoveryCodeHashes { get; private set; } = [];
    public int FailedOtpCount { get; private set; }
    public DateTime? OtpLockedUntilUtc { get; private set; }

    private UserAccount() { } // EF Core

    /// <summary>Creates an account whose password must be changed at first login (default admin).</summary>
    public static UserAccount CreateWithTemporaryPassword(string username, string passwordHash, DateTime nowUtc)
    {
        username = username.Trim();
        if (username.Length == 0) throw new DomainException("Tên đăng nhập không được để trống.");
        if (username.Length > UsernameMaxLength) throw new DomainException($"Tên đăng nhập tối đa {UsernameMaxLength} ký tự.");

        return new UserAccount
        {
            Username = username,
            PasswordHash = passwordHash,
            MustChangePassword = true,
            UpdatedAtUtc = nowUtc,
        };
    }

    public void ChangePassword(string newPasswordHash, DateTime nowUtc)
    {
        PasswordHash = newPasswordHash;
        MustChangePassword = false;
        UpdatedAtUtc = nowUtc;
    }
    public void UpgradePasswordHash(string newPasswordHash, DateTime nowUtc)
    {
        PasswordHash = newPasswordHash;
        UpdatedAtUtc = nowUtc;
    }

    public static void EnsureValidNewPassword(string password)
    {
        if (password.Length < PasswordMinLength)
            throw new DomainException($"Mật khẩu mới phải có ít nhất {PasswordMinLength} ký tự.");
        if (password.Length > PasswordMaxLength)
            throw new DomainException($"Mật khẩu mới tối đa {PasswordMaxLength} ký tự.");
        if (string.IsNullOrWhiteSpace(password))
            throw new DomainException("Mật khẩu mới không được chỉ gồm khoảng trắng.");
    }

    public bool IsOtpLockedAt(DateTime nowUtc) => OtpLockedUntilUtc > nowUtc;

    public void RegisterOtpFailure(DateTime nowUtc)
    {
        FailedOtpCount++;
        if (FailedOtpCount < MaxFailedOtpAttempts) return;
        FailedOtpCount = 0;
        OtpLockedUntilUtc = nowUtc + OtpLockDuration;
    }

    public void ResetOtpFailures()
    {
        FailedOtpCount = 0;
        OtpLockedUntilUtc = null;
    }

    public void ReplaceRecoveryCodes(IEnumerable<string> codeHashes, DateTime nowUtc)
    {
        RecoveryCodeHashes = [.. codeHashes];
        UpdatedAtUtc = nowUtc;
    }

    public bool ConsumeRecoveryCode(Func<string, bool> matches, DateTime nowUtc)
    {
        var hash = RecoveryCodeHashes.FirstOrDefault(matches);
        if (hash is null) return false;
        RecoveryCodeHashes.Remove(hash);
        UpdatedAtUtc = nowUtc;
        return true;
    }
}
