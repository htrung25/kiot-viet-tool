using CommunityToolkit.Mvvm.ComponentModel;

using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace KiotVietTool.Desktop.Services;

public sealed partial class NavigationService(IServiceProvider services, IUserSession session, NavigationRoutes routes)
    : ObservableObject, INavigationService
{
    [ObservableProperty]
    public partial ViewModelBase? CurrentViewModel { get; private set; }

    public Task NavigateToAsync<TViewModel>(object? parameter = null, CancellationToken cancellationToken = default)
        where TViewModel : ViewModelBase =>
        NavigateAsync(typeof(TViewModel), parameter, cancellationToken);

    public Task NavigateHomeAsync(CancellationToken cancellationToken = default) =>
        NavigateAsync(routes.Home, null, cancellationToken);

    async Task NavigateAsync(Type requested, object? parameter, CancellationToken cancellationToken)
    {
        var target = ResolveTarget(requested);
        var viewModel = (ViewModelBase)services.GetRequiredService(target);
        CurrentViewModel = viewModel;
        await viewModel.OnNavigatedToAsync(target == requested ? parameter : null, cancellationToken);
    }

    // Auth guard for every screen: not signed in → Login; temporary password → Change password.
    Type ResolveTarget(Type requested)
    {
        if (typeof(IAllowAnonymous).IsAssignableFrom(requested)) return requested;
        if (session.CurrentUser is not { } user) return routes.Login;
        if (user.MustChangePassword) return routes.ChangePassword;
        return requested;
    }
}
