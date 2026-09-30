using System.ComponentModel;

namespace KiotVietTool.Desktop.Common.Navigation;

/// <summary>ViewModel-first navigation: the view is resolved by Application.DataTemplates in App.axaml.</summary>
public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentViewModel { get; }

    Task NavigateToAsync<TViewModel>(object? parameter = null, CancellationToken cancellationToken = default)
        where TViewModel : ViewModelBase;

    /// <summary>Goes to <see cref="NavigationRoutes.Home"/> (the auth guard still applies).</summary>
    Task NavigateHomeAsync(CancellationToken cancellationToken = default);
}
