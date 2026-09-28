namespace KiotVietTool.Application.Features.Auth;

internal sealed class UserSession : IUserSession
{
    public SignedInUser? CurrentUser { get; private set; }

    public event EventHandler? Changed;

    public void Set(SignedInUser? user)
    {
        CurrentUser = user;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
