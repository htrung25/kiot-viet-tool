using System.Windows;

namespace KiotVietTool.Desktop.Services;

public sealed class DialogService : IDialogService
{
    public void ShowInfo(string message) => Show(message, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowError(string message) => Show(message, MessageBoxButton.OK, MessageBoxImage.Error);

    public bool Confirm(string message) =>
        Show(message, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    static MessageBoxResult Show(string message, MessageBoxButton buttons, MessageBoxImage icon)
    {
        var owner = System.Windows.Application.Current.MainWindow;
        return owner is null
            ? MessageBox.Show(message, "", buttons, icon)
            : MessageBox.Show(owner, message, owner.Title, buttons, icon);
    }
}
