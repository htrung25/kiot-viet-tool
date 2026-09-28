using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiotVietTool.Application.Features.Auth;
using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

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
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(Username));
        };
    }

    public INavigationService Navigation { get; }
    public bool IsAuthenticated => _session.IsAuthenticated;
    public string? Username => _session.CurrentUser?.Username;

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
