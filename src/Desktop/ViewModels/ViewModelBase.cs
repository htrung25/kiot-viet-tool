using CommunityToolkit.Mvvm.ComponentModel;

namespace KiotVietTool.Desktop.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    /// <summary>Called by <see cref="Services.INavigationService"/> after this view model becomes current.</summary>
    public virtual Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken) => Task.CompletedTask;
}
