using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Services;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class LockScreenViewModel(
    IdleLockService idleLock,
    IAuthService authService,
    IUserSessionService session,
    INavigationService navigation,
    ILogger<LockScreenViewModel> logger) : ObservableObject
{
    public const int MaxFailedAttempts = 5;

    int _failedAttempts;

    [ObservableProperty] public partial string Password { get; set; } = "";
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    public string? Username => session.CurrentUser?.Username;
    public string Message => $"Tool đã tự khoá sau {idleLock.LockAfterMinutes} phút không có thao tác. Nhập mật khẩu để tiếp tục, dữ liệu đang làm dở được giữ nguyên.";

    [RelayCommand]
    async Task UnlockAsync(CancellationToken cancellationToken)
    {
        var result = await authService.VerifyCurrentUserPasswordAsync(Password, cancellationToken);
        Password = "";
        if (result.IsSuccess)
        {
            _failedAttempts = 0;
            ErrorMessage = null;
            idleLock.Unlock();
            return;
        }

        if (++_failedAttempts >= MaxFailedAttempts)
        {
            logger.LogWarning("Signed out after {Attempts} failed unlock attempts", _failedAttempts);
            await SignOutCoreAsync(cancellationToken);
            return;
        }
        ErrorMessage = $"{result.Error} Còn {MaxFailedAttempts - _failedAttempts} lần thử trước khi bị đăng xuất.";
    }

    [RelayCommand]
    Task SignOutAsync(CancellationToken cancellationToken) => SignOutCoreAsync(cancellationToken);

    async Task SignOutCoreAsync(CancellationToken cancellationToken)
    {
        _failedAttempts = 0;
        ErrorMessage = null;
        Password = "";
        authService.SignOut();
        await navigation.NavigateToAsync<LoginViewModel>(null, cancellationToken);
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Username));
        OnPropertyChanged(nameof(Message));
    }
}
