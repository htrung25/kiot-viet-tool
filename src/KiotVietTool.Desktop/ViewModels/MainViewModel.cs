using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

public sealed class MainViewModel(INavigationService navigation)
{
    public INavigationService Navigation => navigation;
}
