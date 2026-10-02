namespace KiotVietTool.Application.DTOs;

public sealed record SignInResultDto(bool OtpRequired, string? Notice);
