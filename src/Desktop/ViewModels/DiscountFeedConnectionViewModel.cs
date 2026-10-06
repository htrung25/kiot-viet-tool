using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Models;
using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class DiscountFeedConnectionViewModel(
    IDiscountFeedConnectionService connections,
    IDiscountFeedService feed,
    IDialogService dialogs,
    INotificationService notifications) : ViewModelBase
{
    const string HiddenToken = "••••••••••••••••••••••••";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected), nameof(IsHealthy), nameof(StatusText), nameof(WorkerUrl), nameof(HasReadToken),
        nameof(ReadTokenText), nameof(DeployButtonText))]
    public partial DiscountFeedConnectionDto? Connection { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConflict), nameof(ConflictText), nameof(IsHealthy), nameof(StatusText))]
    public partial DiscountFeedStatusDto FeedStatus { get; private set; } = feed.Status;

    [ObservableProperty] public partial bool IsCloudflareMode { get; set; } = true;

    [ObservableProperty] public partial string ApiToken { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAccounts))]
    public partial IReadOnlyList<FilterOption<string>> Accounts { get; private set; } = [];

    [ObservableProperty] public partial FilterOption<string>? SelectedAccount { get; set; }
    [ObservableProperty] public partial string ScriptName { get; set; } = "";

    [ObservableProperty] public partial string ManualUrl { get; set; } = "";
    [ObservableProperty] public partial string ManualWriteToken { get; set; } = "";
    [ObservableProperty] public partial string ManualReadToken { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadTokenText), nameof(ReadTokenButtonText))]
    public partial bool IsReadTokenVisible { get; set; }

    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string? SuccessMessage { get; set; }
    [ObservableProperty] public partial string? ProgressText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }

    public bool IsIdle => !IsBusy;
    public bool IsConnected => Connection is not null;
    public bool IsHealthy => IsConnected && !HasConflict;
    public bool HasConflict => FeedStatus.Conflict is not null;
    public string? ConflictText => FeedStatus.Conflict is { } conflict ? DisplayFormat.FeedConflict(conflict) : null;
    public string WorkerUrl => Connection?.WorkerUrl ?? "";
    public bool HasReadToken => Connection?.ReadToken is not null;
    public string ReadTokenText => IsReadTokenVisible ? Connection?.ReadToken ?? "" : HiddenToken;
    public string ReadTokenButtonText => IsReadTokenVisible ? "Ẩn mã" : "Hiện mã";
    public bool HasAccounts => Accounts.Count > 0;
    public string DeployButtonText => Connection is { IsManaged: true } ? "Cập nhật Worker" : "Cài lên Cloudflare";

    public string StatusText => Connection is null
        ? "Chưa có Worker giảm giá, máy thu ngân chưa nhận được chương trình nào."
        : FeedStatus switch
        {
            { Conflict: not null } => "Tạm ngừng gửi danh sách giảm giá do xung đột với dữ liệu trên Worker.",
            { LastError: { } error } => $"Gửi lỗi: {error}",
            { LastPublishedAtUtc: { } at } => $"Đã gửi danh sách giảm giá lúc {DisplayFormat.DateTime(at)}.",
            _ => "Danh sách giảm giá trên Worker đã khớp với tool.",
        };

    public override async Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        await ReloadAsync(cancellationToken);
        ScriptName = await connections.SuggestScriptNameAsync(cancellationToken);
        IsCloudflareMode = Connection is null or { IsManaged: true };
        ManualUrl = Connection is { IsManaged: false } manual ? manual.WorkerUrl : "";
    }

    [RelayCommand]
    Task LoadAccountsAsync(CancellationToken cancellationToken) => RunBusyAsync("Đang kiểm tra API Token…", async () =>
    {
        var result = await connections.GetCloudflareAccountsAsync(ApiToken, cancellationToken);
        if (!result.IsSuccess)
        {
            Accounts = [];
            SelectedAccount = null;
            ErrorMessage = result.Error;
            return;
        }
        Accounts = [.. result.Value!.Select(a => new FilterOption<string>(a.Name, a.Id))];
        SelectedAccount = Accounts.FirstOrDefault(a => a.Value == Connection?.CloudflareAccountId) ?? Accounts[0];
    });

    [RelayCommand]
    async Task DeployAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        SuccessMessage = null;
        if (SelectedAccount is not { } account)
        {
            ErrorMessage = "Dán API Token rồi bấm Kiểm tra token để chọn tài khoản Cloudflare.";
            return;
        }
        var scriptName = ScriptName.Trim().ToLowerInvariant();
        if (Connection is { } current && (current.ScriptName != scriptName || current.CloudflareAccountId != account.Value))
        {
            var confirmed = await dialogs.ConfirmAsync("Chuyển sang Worker mới?",
                $"Tool sẽ gửi danh sách giảm giá tới Worker \"{scriptName}\" thay cho {current.WorkerUrl}. "
                + "Mỗi máy thu ngân phải nhập lại địa chỉ và mã đọc mới, nếu không sẽ ngừng giảm giá.",
                "Chuyển sang Worker mới", destructive: true);
            if (!confirmed) return;
        }

        await RunBusyAsync("Đang cài Worker lên Cloudflare…", async () =>
        {
            var result = await connections.DeployAsync(new DeployDiscountFeedWorkerDto(ApiToken, account.Value, scriptName), cancellationToken);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error;
                return;
            }
            ApiToken = "";
            Accounts = [];
            SelectedAccount = null;
            IsReadTokenVisible = true;
            SuccessMessage = result.Value;
            await ReloadAsync(cancellationToken);
        });
    }

    [RelayCommand]
    Task SaveManualAsync(CancellationToken cancellationToken) => RunBusyAsync("Đang kiểm tra Worker…", async () =>
    {
        var result = await connections.SaveManualAsync(
            new SaveDiscountFeedConnectionDto(ManualUrl, ManualWriteToken, ManualReadToken), cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return;
        }
        ManualWriteToken = ManualReadToken = "";
        await ReloadAsync(cancellationToken);
        ManualUrl = WorkerUrl;
        notifications.ShowSuccess(result.Value!);
    });

    [RelayCommand]
    async Task TakeOverAsync(CancellationToken cancellationToken)
    {
        var confirmed = await dialogs.ConfirmAsync("Ghi đè danh sách giảm giá trên Worker?",
            "Danh sách trên Worker sẽ được thay bằng các chương trình đang áp dụng trong tool trên máy này; những gì máy khác đã gửi sẽ mất. "
            + "Máy thu ngân áp dụng danh sách mới trong khoảng 1–2 phút.",
            "Ghi đè bằng máy này", destructive: true);
        if (!confirmed) return;

        await RunBusyAsync("Đang gửi danh sách giảm giá…", async () =>
        {
            var result = await feed.TakeOverAsync(cancellationToken);
            await ReloadAsync(cancellationToken);
            if (result.IsSuccess) notifications.ShowSuccess(result.Value!);
            else ErrorMessage = result.Error;
        });
    }

    [RelayCommand]
    async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var confirmed = await dialogs.ConfirmAsync("Ngắt kết nối máy thu ngân?",
            "Tool sẽ ngừng gửi danh sách giảm giá. Worker trên Cloudflare và danh sách đang có trên đó không bị xoá.",
            "Ngắt kết nối", destructive: true);
        if (!confirmed) return;

        await RunBusyAsync(null, async () =>
        {
            var result = await connections.DisconnectAsync(cancellationToken);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error;
                return;
            }
            IsReadTokenVisible = false;
            await ReloadAsync(cancellationToken);
            notifications.ShowSuccess(result.Value!);
        });
    }

    [RelayCommand]
    void ToggleReadToken() => IsReadTokenVisible = !IsReadTokenVisible;

    async Task ReloadAsync(CancellationToken cancellationToken)
    {
        Connection = await connections.GetAsync(cancellationToken);
        FeedStatus = feed.Status;
    }

    async Task RunBusyAsync(string? progress, Func<Task> action)
    {
        ErrorMessage = null;
        SuccessMessage = null;
        ProgressText = progress;
        IsBusy = true;
        try { await action(); }
        finally
        {
            IsBusy = false;
            ProgressText = null;
        }
    }
}
