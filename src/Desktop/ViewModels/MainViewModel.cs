using System.ComponentModel;

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

    public MainViewModel(INavigationService navigation, IUserSessionService session, IAuthService authService,
        IdleLockService idleLock, LockScreenViewModel lockScreen)
    {
        Navigation = navigation;
        _session = session;
        _authService = authService;
        _lockScreen = lockScreen;
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
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(UserInitial));
        };
        Navigation.PropertyChanged += OnNavigationChanged;
    }

    public INavigationService Navigation { get; }
    public IdleLockService IdleLock { get; }
    public LockScreenViewModel? LockScreen => IdleLock.IsLocked ? _lockScreen : null;

    /// <summary>Sidebar is hidden on Login and on the forced first-login password change (full-screen flows).</summary>
    public bool ShowChrome => _session.CurrentUser is { MustChangePassword: false };

    public string? Username => _session.CurrentUser?.Username;
    public string UserInitial => Username is { Length: > 0 } name ? name[..1].ToUpperInvariant() : "";

    public bool IsProductsActive => Navigation.CurrentViewModel is ProductListViewModel;
    public bool IsConnectionActive => Navigation.CurrentViewModel is KiotVietConnectionViewModel;
    public bool IsProgramsActive => Navigation.CurrentViewModel is DiscountProgramListViewModel or DiscountProgramEditorViewModel
        or DiscountProgramDetailViewModel;

    void OnNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentViewModel))
        {
            OnPropertyChanged(nameof(IsProductsActive));
            OnPropertyChanged(nameof(IsConnectionActive));
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
    Task ChangePasswordAsync(CancellationToken cancellationToken) =>
        Navigation.NavigateToAsync<ChangePasswordViewModel>(null, cancellationToken);

    [RelayCommand]
    Task SignOutAsync(CancellationToken cancellationToken)
    {
        _authService.SignOut();
        return Navigation.NavigateToAsync<LoginViewModel>(null, cancellationToken);
    }
}
