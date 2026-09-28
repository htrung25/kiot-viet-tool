namespace KiotVietTool.Infrastructure.Auth;

/// <summary>Default admin created on first start when no account exists; the password must be changed at first login.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string DefaultAdminUsername { get; set; } = "";
    public string DefaultAdminPassword { get; set; } = "";
}
