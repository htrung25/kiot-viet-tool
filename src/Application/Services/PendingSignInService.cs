namespace KiotVietTool.Application.Services;

internal sealed class PendingSignInService
{
    public sealed record Challenge(int AccountId, string Code, DateTime ExpiresAtUtc, DateTime SentAtUtc, int Attempts);

    public Challenge? Current { get; set; }
}
