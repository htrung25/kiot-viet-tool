using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;

namespace KiotVietTool.Application.Services;

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
