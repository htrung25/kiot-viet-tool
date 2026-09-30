using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Models;
using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class KiotVietConnectionViewModel(
    IKiotVietConnectionService connections,
    ICatalogSyncService sync,
    IDialogService dialogs,
    INotificationService notifications) : ViewModelBase
{
    [ObservableProperty] public partial string Retailer { get; set; } = "";
    [ObservableProperty] public partial string ClientId { get; set; } = "";
    [ObservableProperty] public partial string ClientSecret { get; set; } = "";
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string? SuccessMessage { get; set; }
    [ObservableProperty] public partial string? ProgressText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SecretPlaceholder), nameof(StatusText))]
    public partial KiotVietConnectionDto? Connection { get; private set; }

    public bool IsIdle => !IsBusy;
    public bool IsConnected => Connection is not null;
    public string SecretPlaceholder => IsConnected ? "••••••  (để trống nếu không đổi)" : "Dán Client Secret từ KiotViet";
    public string StatusText => Connection is { } c
        ? $"Đã kết nối gian hàng {c.Retailer} · Đồng bộ gần nhất: {DisplayFormat.DateTime(c.LastSyncedAtUtc)}"
        : "Chưa cấu hình kết nối";

    partial void OnConnectionChanged(KiotVietConnectionDto? value) => OnPropertyChanged(nameof(IsConnected));

    public override async Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        Connection = await connections.GetAsync(cancellationToken);
        Retailer = Connection?.Retailer ?? "";
        ClientId = Connection?.ClientId ?? "";
        ClientSecret = "";
    }

    [RelayCommand]
    Task TestAsync(CancellationToken cancellationToken) => RunBusyAsync(async () =>
    {
        var result = await connections.TestAsync(Request(), cancellationToken);
        if (result.IsSuccess)
            SuccessMessage = $"Kết nối thành công. Gian hàng có {DisplayFormat.Number(result.Value)} sản phẩm.";
        else ErrorMessage = result.Error;
    });

    [RelayCommand]
    Task SaveAsync(CancellationToken cancellationToken) => RunBusyAsync(async () =>
    {
        var result = await connections.SaveAsync(Request(), cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return;
        }

        ClientSecret = "";
        Connection = await connections.GetAsync(cancellationToken);
        notifications.ShowSuccess("Đã lưu kết nối KiotViet.");
        await SyncCoreAsync(cancellationToken);
    });

    [RelayCommand]
    Task SyncAsync(CancellationToken cancellationToken) => RunBusyAsync(() => SyncCoreAsync(cancellationToken));

    [RelayCommand]
    async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var confirmed = await dialogs.ConfirmAsync("Ngắt kết nối KiotViet?",
            "Thông tin kết nối và toàn bộ dữ liệu đã đồng bộ (nhóm hàng, sản phẩm, bảng giá) sẽ bị xoá khỏi máy này. Dữ liệu trên KiotViet không bị ảnh hưởng.",
            "Ngắt kết nối", destructive: true);
        if (!confirmed) return;

        await RunBusyAsync(async () =>
        {
            var result = await connections.DisconnectAsync(cancellationToken);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error;
                return;
            }
            Connection = null;
            Retailer = ClientId = ClientSecret = "";
            notifications.ShowSuccess("Đã ngắt kết nối KiotViet.");
        });
    }

    async Task SyncCoreAsync(CancellationToken cancellationToken)
    {
        var progress = new Progress<SyncProgressDto>(p => ProgressText = p.Total is { } total
            ? $"Đang đồng bộ {p.Stage.ToLowerInvariant()}: {DisplayFormat.Number(p.Done)} / {DisplayFormat.Number(total)}"
            : $"Đang đồng bộ {p.Stage.ToLowerInvariant()}…");
        var result = await sync.SyncAsync(progress, cancellationToken);
        ProgressText = null;
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return;
        }

        Connection = await connections.GetAsync(cancellationToken);
        var r = result.Value!;
        notifications.ShowSuccess(r.WasFullSync
            ? $"Đã đồng bộ {DisplayFormat.Number(r.ChangedProducts)} sản phẩm và {DisplayFormat.Number(r.PriceBooks)} bảng giá."
            : $"Đã đồng bộ: {DisplayFormat.Number(r.ChangedProducts)} sản phẩm thay đổi, {DisplayFormat.Number(r.RemovedProducts)} sản phẩm bị xoá.");
    }

    SaveKiotVietConnectionDto Request() => new(Retailer, ClientId, ClientSecret);

    async Task RunBusyAsync(Func<Task> action)
    {
        ErrorMessage = null;
        SuccessMessage = null;
        IsBusy = true;
        try { await action(); }
        finally { IsBusy = false; }
    }
}
