using Avalonia.Controls.ApplicationLifetimes;

namespace KiotVietTool.Desktop.Common.Dialogs;

public sealed class DialogService : IDialogService
{
    public Task ShowInfoAsync(string message) => ShowAsync(message, MessageDialogKind.Info);

    public Task ShowErrorAsync(string message) => ShowAsync(message, MessageDialogKind.Error);

    public Task<bool> ConfirmAsync(string message) => ShowAsync(message, MessageDialogKind.Confirm);

    static async Task<bool> ShowAsync(string message, MessageDialogKind kind)
    {
        var owner = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        var dialog = new MessageDialog(message, owner?.Title ?? "", kind);
        if (owner is { IsVisible: true }) return await dialog.ShowDialog<bool>(owner);

        // No visible owner (e.g. startup failure): show standalone and wait for it to close.
        var closed = new TaskCompletionSource<bool>();
        dialog.Closed += (_, _) => closed.TrySetResult(dialog.Result);
        dialog.Show();
        return await closed.Task;
    }
}
