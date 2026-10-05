using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Models;
using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class TelegramSettingsViewModel(ITelegramService telegram, INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected), nameof(BotLink), nameof(ConnectedAtText), nameof(RecoveryCodesText), nameof(IsRecoveryLow))]
    public partial TelegramStatusDto? Status { get; private set; }

    public bool IsConnected => Status is not null;
    public string BotLink => Status is null ? "" : $"@{Status.BotUsername}";
    public string ConnectedAtText => Status is null ? "" : VietnamTime.FromUtc(Status.ConnectedAtUtc).ToString("dd/MM/yyyy HH:mm");
    public string RecoveryCodesText => Status is null ? "" : $"{Status.RecoveryCodesLeft}/10 mã";
    public bool IsRecoveryLow => Status is { RecoveryCodesLeft: <= 3 };

    public IReadOnlyList<NotificationEventChoice> Events { get; } =
    [
        new("Chương trình bắt đầu / kết thúc", "Máy thu ngân bắt đầu hoặc ngừng giảm giá.", true),
        new("Không gửi được tới máy thu ngân", "Danh sách giảm giá mới chưa tới được máy thu ngân.", true),
        new("Đồng bộ thất bại", "Không đồng bộ được sản phẩm / bảng giá từ KiotViet.", false),
    ];

    public override async Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken) =>
        Status = await telegram.GetStatusAsync(cancellationToken);

    [RelayCommand]
    Task ReconnectAsync(CancellationToken cancellationToken) =>
        navigation.NavigateToAsync<TelegramSetupViewModel>(null, cancellationToken);
}
