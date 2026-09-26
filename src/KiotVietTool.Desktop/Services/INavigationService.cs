using System.ComponentModel;
using KiotVietTool.Desktop.ViewModels;

namespace KiotVietTool.Desktop.Services;

/// <summary>ViewModel-first navigation: the view is resolved by DataTemplate (Resources/DataTemplates.xaml).</summary>
public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentViewModel { get; }

    Task NavigateToAsync<TViewModel>(object? parameter = null, CancellationToken cancellationToken = default)
        where TViewModel : ViewModelBase;
}
