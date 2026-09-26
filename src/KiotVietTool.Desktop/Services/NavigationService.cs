using CommunityToolkit.Mvvm.ComponentModel;
using KiotVietTool.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KiotVietTool.Desktop.Services;

public sealed partial class NavigationService(IServiceProvider services) : ObservableObject, INavigationService
{
    [ObservableProperty]
    public partial ViewModelBase? CurrentViewModel { get; private set; }

    public async Task NavigateToAsync<TViewModel>(object? parameter = null, CancellationToken cancellationToken = default)
        where TViewModel : ViewModelBase
    {
        var viewModel = services.GetRequiredService<TViewModel>();
        CurrentViewModel = viewModel;
        await viewModel.OnNavigatedToAsync(parameter, cancellationToken);
    }
}
