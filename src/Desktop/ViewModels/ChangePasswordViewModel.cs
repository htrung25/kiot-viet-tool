using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class ChangePasswordViewModel(
    IAuthService authService,
    IUserSession session,
    INavigationService navigation,
    INotificationService notifications) : ViewModelBase
{
    /// <summary>First login with the temporary password: cannot skip, cancelling signs out.</summary>
    public bool IsForced => session.CurrentUser?.MustChangePassword == true;

    [ObservableProperty] public partial string CurrentPassword { get; set; } = "";
    [ObservableProperty] public partial string NewPassword { get; set; } = "";
    [ObservableProperty] public partial string ConfirmPassword { get; set; } = "";
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    async Task SaveAsync(CancellationToken cancellationToken)
    {
        var result = await authService.ChangePasswordAsync(
            new ChangePasswordRequest(CurrentPassword, NewPassword, ConfirmPassword), cancellationToken);
        ErrorMessage = result.Error;
        if (!result.IsSuccess) return;

        notifications.ShowSuccess("Đã đổi mật khẩu.");
        await navigation.NavigateHomeAsync(cancellationToken);
    }

    [RelayCommand]
    async Task CancelAsync(CancellationToken cancellationToken)
    {
        if (IsForced)
        {
            authService.SignOut();
            await navigation.NavigateToAsync<LoginViewModel>(null, cancellationToken);
            return;
        }
        await navigation.NavigateHomeAsync(cancellationToken);
    }
}
