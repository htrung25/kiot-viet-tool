using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Features.Auth;
using KiotVietTool.Desktop.Common.Navigation;
using KiotVietTool.Desktop.Features.Auth;
using KiotVietTool.Desktop.Features.Customers;

namespace KiotVietTool.Desktop.Shell;

public sealed partial class MainViewModel : ObservableObject
{
    readonly IUserSession _session;
    readonly IAuthService _authService;

    public MainViewModel(INavigationService navigation, IUserSession session, IAuthService authService)
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

    public bool IsCustomersActive => Navigation.CurrentViewModel is CustomerListViewModel or CustomerEditViewModel;

    void OnNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentViewModel))
            OnPropertyChanged(nameof(IsCustomersActive));
    }

    [RelayCommand]
    Task OpenCustomersAsync(CancellationToken cancellationToken) =>
        Navigation.NavigateToAsync<CustomerListViewModel>(null, cancellationToken);

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
