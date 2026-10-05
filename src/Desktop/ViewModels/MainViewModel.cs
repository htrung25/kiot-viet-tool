using System.ComponentModel;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    readonly IUserSessionService _session;
    readonly IAuthService _authService;
    readonly LockScreenViewModel _lockScreen;
    readonly IServerClockService _clock;

    public MainViewModel(INavigationService navigation, IUserSessionService session, IAuthService authService,
        IdleLockService idleLock, LockScreenViewModel lockScreen, IServerClockService clock)
    {
        Navigation = navigation;
        _session = session;
        _authService = authService;
        _lockScreen = lockScreen;
        _clock = clock;
        _clock.OffsetChanged += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(ClockWarning));
            OnPropertyChanged(nameof(ShowClockWarning));
        });
        IdleLock = idleLock;
        IdleLock.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(IdleLockService.IsLocked)) return;
            _lockScreen.Refresh();
            OnPropertyChanged(nameof(LockScreen));
        };
        _session.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(ShowChrome));
            OnPropertyChanged(nameof(ShowClockWarning));
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(UserInitial));
        };
        Navigation.PropertyChanged += OnNavigationChanged;
    }

    public INavigationService Navigation { get; }
    public IdleLockService IdleLock { get; }
    public LockScreenViewModel? LockScreen => IdleLock.IsLocked ? _lockScreen : null;

    /// <summary>Sidebar is hidden on Login, the forced first-login password change and the mandatory Telegram setup (full-screen flows).</summary>
    public bool ShowChrome => _session.CurrentUser is { MustChangePassword: false, NeedsTelegram: false }
        && Navigation.CurrentViewModel is not TelegramSetupViewModel { IsMandatory: true };

    public bool ShowClockWarning => ShowChrome && _clock.IsSkewed;

    public string ClockWarning
    {
        get
        {
            var offset = _clock.Offset;
            var minutes = Math.Round(offset.Duration().TotalMinutes);
            return $"Giờ máy tính đang {(offset > TimeSpan.Zero ? "chậm" : "nhanh")} {minutes:0} phút so với KiotViet. "
                + "Tool vẫn tính giờ chương trình theo giờ KiotViet, nhưng hãy chỉnh lại giờ Windows "
                + "(Cài đặt → Thời gian & ngôn ngữ → Ngày & giờ → Đồng bộ ngay).";
        }
    }

    public string? Username => _session.CurrentUser?.Username;
    public string UserInitial => Username is { Length: > 0 } name ? name[..1].ToUpperInvariant() : "";

    public bool IsProductsActive => Navigation.CurrentViewModel is ProductListViewModel;
    public bool IsConnectionActive => Navigation.CurrentViewModel is KiotVietConnectionViewModel;
    public bool IsTelegramActive => Navigation.CurrentViewModel is TelegramSettingsViewModel or TelegramSetupViewModel;
    public bool IsProgramsActive => Navigation.CurrentViewModel is DiscountProgramListViewModel or DiscountProgramEditorViewModel
        or DiscountProgramDetailViewModel;

    void OnNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentViewModel))
        {
            OnPropertyChanged(nameof(ShowChrome));
            OnPropertyChanged(nameof(ShowClockWarning));
            OnPropertyChanged(nameof(IsProductsActive));
            OnPropertyChanged(nameof(IsConnectionActive));
            OnPropertyChanged(nameof(IsTelegramActive));
            OnPropertyChanged(nameof(IsProgramsActive));
        }
    }

    [RelayCommand]
    Task OpenProductsAsync(CancellationToken cancellationToken) =>
        Navigation.NavigateToAsync<ProductListViewModel>(null, cancellationToken);

    [RelayCommand]
    Task OpenProgramsAsync(CancellationToken cancellationToken) =>
        Navigation.NavigateToAsync<DiscountProgramListViewModel>(null, cancellationToken);

    [RelayCommand]
    Task OpenConnectionAsync(CancellationToken cancellationToken) =>
        Navigation.NavigateToAsync<KiotVietConnectionViewModel>(null, cancellationToken);

    [RelayCommand]
    Task OpenTelegramAsync(CancellationToken cancellationToken) =>
        Navigation.NavigateToAsync<TelegramSettingsViewModel>(null, cancellationToken);

    [RelayCommand]
    Task ChangePasswordAsync(CancellationToken cancellationToken) =>
        Navigation.NavigateToAsync<ChangePasswordViewModel>(null, cancellationToken);

    [RelayCommand]
    Task SignOutAsync(CancellationToken cancellationToken)
    {
        _authService.SignOut();
        return Navigation.NavigateToAsync<LoginViewModel>(null, cancellationToken);
    }
}
