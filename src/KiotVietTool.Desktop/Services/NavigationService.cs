using CommunityToolkit.Mvvm.ComponentModel;
using KiotVietTool.Application.Features.Auth;
using KiotVietTool.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KiotVietTool.Desktop.Services;

public sealed partial class NavigationService(IServiceProvider services, IUserSession session)
    : ObservableObject, INavigationService
{
    [ObservableProperty]
    public partial ViewModelBase? CurrentViewModel { get; private set; }

    public async Task NavigateToAsync<TViewModel>(object? parameter = null, CancellationToken cancellationToken = default)
        where TViewModel : ViewModelBase
    {
        var target = ResolveTarget(typeof(TViewModel));
        var viewModel = (ViewModelBase)services.GetRequiredService(target);
        CurrentViewModel = viewModel;
        await viewModel.OnNavigatedToAsync(target == typeof(TViewModel) ? parameter : null, cancellationToken);
    }

    // Auth guard for every screen: not signed in → Login; temporary password → Change password.
    Type ResolveTarget(Type requested)
    {
        if (typeof(IAllowAnonymous).IsAssignableFrom(requested)) return requested;
        if (session.CurrentUser is not { } user) return typeof(LoginViewModel);
        if (user.MustChangePassword) return typeof(ChangePasswordViewModel);
        return requested;
    }
}
