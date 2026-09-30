using System.ComponentModel;

using KiotVietTool.Desktop.ViewModels;

namespace KiotVietTool.Desktop.Services;

/// <summary>ViewModel-first navigation: the view is resolved by Application.DataTemplates in App.axaml.</summary>
public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentViewModel { get; }

    Task NavigateToAsync<TViewModel>(object? parameter = null, CancellationToken cancellationToken = default)
        where TViewModel : ViewModelBase;

    /// <summary>Goes to <see cref="Models.NavigationRoutes.Home"/> (the auth guard still applies).</summary>
    Task NavigateHomeAsync(CancellationToken cancellationToken = default);
}
