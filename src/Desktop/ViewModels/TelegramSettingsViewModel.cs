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
        new("Đã áp giá giảm", "Chương trình bắt đầu, giá bán trên KiotViet đã đổi.", true),
        new("Đã trả giá gốc", "Chương trình kết thúc hoặc bị dừng, giá đã về giá gốc.", true),
        new("Lỗi áp giá / trả giá", "Có sản phẩm chưa đổi được giá, cần bấm Thử lại.", true),
        new("Quá hạn chưa trả giá", "Đã qua giờ kết thúc nhưng giá giảm vẫn còn trên KiotViet.", true),
        new("Giá bị sửa tay trên KiotViet", "Tool không ghi đè, cần chọn giữ giá hay trả về giá gốc.", true),
        new("Đồng bộ thất bại", "Không đồng bộ được sản phẩm / bảng giá từ KiotViet.", false),
    ];

    public override async Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken) =>
        Status = await telegram.GetStatusAsync(cancellationToken);

    [RelayCommand]
    Task ReconnectAsync(CancellationToken cancellationToken) =>
        navigation.NavigateToAsync<TelegramSetupViewModel>(null, cancellationToken);
}
