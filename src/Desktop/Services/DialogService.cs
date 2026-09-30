using Avalonia.Controls.ApplicationLifetimes;

using KiotVietTool.Desktop.Models;
using KiotVietTool.Desktop.Views;

namespace KiotVietTool.Desktop.Services;

public sealed class DialogService : IDialogService
{
    public Task ShowInfoAsync(string message) =>
        ShowAsync(new MessageDialogWindow("Thông báo", message, MessageDialogKind.Info, "Đóng"));

    public Task ShowErrorAsync(string message) =>
        ShowAsync(new MessageDialogWindow("Có lỗi xảy ra", message, MessageDialogKind.Error, "Đóng"));

    public Task<bool> ConfirmAsync(string title, string message, string confirmText, bool destructive = false) =>
        ShowAsync(new MessageDialogWindow(title, message,
            destructive ? MessageDialogKind.DestructiveConfirm : MessageDialogKind.Confirm, confirmText));

    static async Task<bool> ShowAsync(MessageDialogWindow dialog)
    {
        var owner = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        dialog.Title = owner?.Title ?? "";
        if (owner is { IsVisible: true }) return await dialog.ShowDialog<bool>(owner);

        // No visible owner (e.g. startup failure): show standalone and wait for it to close.
        var closed = new TaskCompletionSource<bool>();
        dialog.Closed += (_, _) => closed.TrySetResult(dialog.Result);
        dialog.Show();
        return await closed.Task;
    }
}
