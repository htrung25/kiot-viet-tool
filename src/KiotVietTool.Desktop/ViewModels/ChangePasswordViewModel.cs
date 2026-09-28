using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiotVietTool.Application.Features.Auth;
using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class ChangePasswordViewModel(
    IAuthService authService,
    IUserSession session,
    INavigationService navigation,
    IDialogService dialog) : ViewModelBase
{
    /// <summary>First login with the temporary password: cannot skip, cancelling signs out.</summary>
    public bool IsForced => session.CurrentUser?.MustChangePassword == true;

    public string Hint => IsForced
        ? "Bạn đang dùng mật khẩu mặc định. Hãy đặt mật khẩu mới trước khi sử dụng."
        : "Nhập mật khẩu hiện tại và mật khẩu mới.";

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

        await dialog.ShowInfoAsync("Đã đổi mật khẩu.");
        await navigation.NavigateToAsync<CustomerListViewModel>(null, cancellationToken);
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
        await navigation.NavigateToAsync<CustomerListViewModel>(null, cancellationToken);
    }
}
