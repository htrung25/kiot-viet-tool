namespace KiotVietTool.Application.DTOs;

public sealed record SignedInUserDto(int Id, string Username, bool MustChangePassword);
