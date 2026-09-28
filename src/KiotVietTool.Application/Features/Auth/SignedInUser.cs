namespace KiotVietTool.Application.Features.Auth;

public sealed record SignedInUser(int Id, string Username, bool MustChangePassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
