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

    public MainViewModel(INavigationService navigation, IUserSessionService session, IAuthService authService)
    {
        Navigation = navigation;
        _session = session;
        _authService = authService;
        _session.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(ShowChrome));
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(UserInitial));
        };
        Navigation.PropertyChanged += OnNavigationChanged;
    }

    public INavigationService Navigation { get; }

    /// <summary>Sidebar is hidden on Login and on the forced first-login password change (full-screen flows).</summary>
    public bool ShowChrome => _session.CurrentUser is { MustChangePassword: false };

    public string? Username => _session.CurrentUser?.Username;
    public string UserInitial => Username is { Length: > 0 } name ? name[..1].ToUpperInvariant() : "";

    public bool IsProductsActive => Navigation.CurrentViewModel is ProductListViewModel;

    void OnNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentViewModel))
            OnPropertyChanged(nameof(IsProductsActive));
    }

    [RelayCommand]
    Task OpenProductsAsync(CancellationToken cancellationToken) =>
        Navigation.NavigateToAsync<ProductListViewModel>(null, cancellationToken);

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
