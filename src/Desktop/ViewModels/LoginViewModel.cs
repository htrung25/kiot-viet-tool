using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class LoginViewModel(IAuthService authService, INavigationService navigation)
    : ViewModelBase, IAnonymousViewModel
{
    [ObservableProperty] public partial string Username { get; set; } = "";
    [ObservableProperty] public partial string Password { get; set; } = "";
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    async Task SignInAsync(CancellationToken cancellationToken)
    {
        var result = await authService.SignInAsync(Username, Password, cancellationToken);
        Password = "";
        ErrorMessage = result.Error;
        // The navigation guard redirects to Change password when the password is temporary.
        if (result.IsSuccess) await navigation.NavigateHomeAsync(cancellationToken);
    }
}
