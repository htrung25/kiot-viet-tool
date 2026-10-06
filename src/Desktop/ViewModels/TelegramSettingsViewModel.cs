using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Models;
using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class TelegramSettingsViewModel(
    ITelegramService telegram,
    IUserSessionService session,
    INavigationService navigation,
    INotificationService notifications) : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected), nameof(IsOtpEnabled), nameof(IsOtpDisabled), nameof(OtpStatusText), nameof(BotLink),
        nameof(ConnectedAtText), nameof(RecoveryCodesText), nameof(IsRecoveryLow), nameof(ConnectButtonText))]
    public partial TelegramStatusDto? Status { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfirming), nameof(ConfirmText), nameof(ConfirmButtonText))]
    public partial bool? ConfirmingDisconnect { get; private set; }

    [ObservableProperty] public partial string Password { get; set; } = "";
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }

    public bool IsIdle => !IsBusy;
    public bool IsConnected => Status is not null;
    public bool IsOtpEnabled => Status is { IsLoginOtpEnabled: true };
    public bool IsOtpDisabled => Status is { IsLoginOtpEnabled: false };
    public bool IsConfirming => ConfirmingDisconnect is not null;
    public string BotLink => Status is null ? "" : $"@{Status.BotUsername}";
    public string ConnectedAtText => Status is null ? "" : VietnamTime.FromUtc(Status.ConnectedAtUtc).ToString("dd/MM/yyyy HH:mm");
    public string RecoveryCodesText => Status is null ? "" : $"{Status.RecoveryCodesLeft}/10 mã";
    public bool IsRecoveryLow => Status is { IsLoginOtpEnabled: true, RecoveryCodesLeft: <= 3 };
    public string ConnectButtonText => IsConnected ? "Kết nối lại" : "Kết nối Telegram";

    public string OtpStatusText => Status switch
    {
        null => "Chưa kết nối Telegram: đăng nhập chỉ cần mật khẩu. Kết nối bot Telegram để bật mã xác thực 2 lớp (khuyên dùng).",
        { IsLoginOtpEnabled: true } => "Đang bật: sau khi nhập mật khẩu, bot gửi mã 6 số tới tài khoản Telegram dưới đây.",
        _ => "Đang tắt: đăng nhập chỉ cần mật khẩu. Kết nối Telegram vẫn được giữ để bật lại bất cứ lúc nào.",
    };

    public string ConfirmText => ConfirmingDisconnect == true
        ? "Ngắt kết nối Telegram? Bot Token và các mã dự phòng sẽ bị xoá khỏi tool, đăng nhập chỉ cần mật khẩu. Nhập mật khẩu tool để xác nhận."
        : "Tắt mã OTP? Đăng nhập sẽ chỉ cần mật khẩu, không cần mã gửi qua Telegram. Nhập mật khẩu tool để xác nhận.";
    public string ConfirmButtonText => ConfirmingDisconnect == true ? "Ngắt kết nối" : "Tắt mã OTP";

    public IReadOnlyList<NotificationEventChoice> Events { get; } =
    [
        new("Chương trình bắt đầu / kết thúc", "Máy thu ngân bắt đầu hoặc ngừng giảm giá.", true),
        new("Không gửi được tới máy thu ngân", "Danh sách giảm giá mới chưa tới được máy thu ngân.", true),
        new("Đồng bộ thất bại", "Không đồng bộ được sản phẩm / bảng giá từ KiotViet.", false),
    ];

    public override async Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken) =>
        Status = await telegram.GetStatusAsync(cancellationToken);

    [RelayCommand]
    Task ConnectAsync(CancellationToken cancellationToken) =>
        navigation.NavigateToAsync<TelegramSetupViewModel>(null, cancellationToken);

    [RelayCommand]
    Task EnableOtpAsync(CancellationToken cancellationToken) => RunBusyAsync(async () =>
    {
        var result = await telegram.EnableLoginOtpAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return;
        }
        Status = await telegram.GetStatusAsync(cancellationToken);
        notifications.ShowSuccess("Đã bật mã OTP. Từ lần đăng nhập sau sẽ cần mã gửi qua Telegram.");
    });

    [RelayCommand]
    void StartDisableOtp() => StartConfirm(disconnect: false);

    [RelayCommand]
    void StartDisconnect() => StartConfirm(disconnect: true);

    [RelayCommand]
    void CancelConfirm()
    {
        ConfirmingDisconnect = null;
        Password = "";
        ErrorMessage = null;
    }

    [RelayCommand]
    Task ConfirmAsync(CancellationToken cancellationToken) => RunBusyAsync(async () =>
    {
        var disconnect = ConfirmingDisconnect == true;
        var password = Password;
        Password = "";
        var result = disconnect
            ? await telegram.DisconnectAsync(password, cancellationToken)
            : await telegram.DisableLoginOtpAsync(password, cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            // Too many wrong passwords ended the session.
            if (session.CurrentUser is null) await navigation.NavigateToAsync<LoginViewModel>(null, cancellationToken);
            return;
        }

        ConfirmingDisconnect = null;
        Status = await telegram.GetStatusAsync(cancellationToken);
        notifications.ShowSuccess(disconnect ? "Đã ngắt kết nối Telegram." : "Đã tắt mã OTP khi đăng nhập.");
    });

    void StartConfirm(bool disconnect)
    {
        ErrorMessage = null;
        Password = "";
        ConfirmingDisconnect = disconnect;
    }

    async Task RunBusyAsync(Func<Task> action)
    {
        ErrorMessage = null;
        IsBusy = true;
        try { await action(); }
        finally { IsBusy = false; }
    }
}
