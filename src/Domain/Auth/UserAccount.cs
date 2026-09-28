using KiotVietTool.Domain.Common;

namespace KiotVietTool.Domain.Auth;

public sealed class UserAccount
{
    public const int UsernameMaxLength = 50;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public int Id { get; private set; }
    public string Username { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public bool MustChangePassword { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

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

    /// <summary>Password policy, checked on the plain text before hashing.</summary>
    public static void EnsureValidNewPassword(string password)
    {
        if (password.Length < PasswordMinLength)
            throw new DomainException($"Mật khẩu mới phải có ít nhất {PasswordMinLength} ký tự.");
        if (password.Length > PasswordMaxLength)
            throw new DomainException($"Mật khẩu mới tối đa {PasswordMaxLength} ký tự.");
        if (string.IsNullOrWhiteSpace(password))
            throw new DomainException("Mật khẩu mới không được chỉ gồm khoảng trắng.");
    }
}
