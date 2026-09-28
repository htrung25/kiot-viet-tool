using CommunityToolkit.Mvvm.ComponentModel;

namespace KiotVietTool.Desktop.Common;

public abstract class ViewModelBase : ObservableObject
{
    /// <summary>Called by <see cref="Navigation.INavigationService"/> after this view model becomes current.</summary>
    public virtual Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken) => Task.CompletedTask;
}
