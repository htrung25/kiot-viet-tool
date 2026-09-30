using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

/// <summary>Who is signed in to this app instance. Changed only through <see cref="IAuthService"/>.</summary>
public interface IUserSessionService
{
    SignedInUserDto? CurrentUser { get; }
    bool IsAuthenticated => CurrentUser is not null;

    event EventHandler? Changed;
}
