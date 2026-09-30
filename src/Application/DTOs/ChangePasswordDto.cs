namespace KiotVietTool.Application.DTOs;

public sealed record ChangePasswordDto(string CurrentPassword, string NewPassword, string ConfirmPassword);
