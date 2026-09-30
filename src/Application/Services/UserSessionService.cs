using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;

namespace KiotVietTool.Application.Services;

internal sealed class UserSessionService : IUserSessionService
{
    public SignedInUserDto? CurrentUser { get; private set; }

    public event EventHandler? Changed;

    public void Set(SignedInUserDto? user)
    {
        CurrentUser = user;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
