using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class LoginViewModel(IAuthService authService, INavigationService navigation, INotificationService notifications)
    : ViewModelBase, IAnonymousViewModel
{
    [ObservableProperty] public partial string Username { get; set; } = "";
    [ObservableProperty] public partial string Password { get; set; } = "";
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string? Notice { get; set; }
    [ObservableProperty] public partial string OtpCode { get; set; } = "";
    [ObservableProperty] public partial string RecoveryCode { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPasswordStep), nameof(Title), nameof(Subtitle))]
    public partial bool IsOtpStep { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(Subtitle))]
    public partial bool UseRecoveryCode { get; set; }

    public bool IsPasswordStep => !IsOtpStep;
    public string Title => !IsOtpStep ? "Đăng nhập" : UseRecoveryCode ? "Dùng mã dự phòng" : "Nhập mã xác thực";
    public string Subtitle => !IsOtpStep
        ? "Nhập tài khoản quản trị để tiếp tục."
        : UseRecoveryCode
            ? "Nhập một trong các mã dự phòng bạn đã lưu khi kết nối Telegram. Mỗi mã chỉ dùng được một lần."
            : "Mở Telegram và nhập mã 6 số bot vừa gửi.";

    [RelayCommand]
    async Task SignInAsync(CancellationToken cancellationToken)
    {
        var result = await authService.SignInAsync(Username, Password, cancellationToken);
        Password = "";
        ErrorMessage = result.Error;
        if (!result.IsSuccess) return;

        if (result.Value!.OtpRequired)
        {
            OtpCode = "";
            RecoveryCode = "";
            UseRecoveryCode = false;
            Notice = result.Value.Notice;
            IsOtpStep = true;
            return;
        }
        await navigation.NavigateHomeAsync(cancellationToken);
    }

    [RelayCommand]
    async Task VerifyAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        if (UseRecoveryCode)
        {
            var recovery = await authService.SignInWithRecoveryCodeAsync(RecoveryCode, cancellationToken);
            RecoveryCode = "";
            if (!recovery.IsSuccess)
            {
                ErrorMessage = recovery.Error;
                if (!authService.HasPendingSignIn) IsOtpStep = false;
                return;
            }
            notifications.ShowSuccess(recovery.Value!);
        }
        else
        {
            var result = await authService.VerifyOtpAsync(OtpCode, cancellationToken);
            OtpCode = "";
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error;
                if (!authService.HasPendingSignIn) IsOtpStep = false;
                return;
            }
        }
        IsOtpStep = false;
        await navigation.NavigateHomeAsync(cancellationToken);
    }

    [RelayCommand]
    async Task ResendAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        var result = await authService.ResendOtpAsync(cancellationToken);
        if (result.IsSuccess) Notice = result.Value;
        else ErrorMessage = result.Error;
    }

    [RelayCommand]
    void ToggleRecoveryCode()
    {
        ErrorMessage = null;
        UseRecoveryCode = !UseRecoveryCode;
    }

    [RelayCommand]
    void BackToPassword()
    {
        authService.CancelPendingSignIn();
        ErrorMessage = null;
        Notice = null;
        IsOtpStep = false;
    }
}
