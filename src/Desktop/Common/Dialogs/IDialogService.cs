namespace KiotVietTool.Desktop.Common.Dialogs;

public interface IDialogService
{
    Task ShowInfoAsync(string message);
    Task ShowErrorAsync(string message);

    /// <param name="title">Question as heading, e.g. "Xoá khách hàng?".</param>
    /// <param name="message">Consequence of confirming.</param>
    /// <param name="confirmText">Verb for the confirm button, e.g. "Xoá" (never a vague "OK").</param>
    /// <param name="destructive">Red confirm button for irreversible actions.</param>
    Task<bool> ConfirmAsync(string title, string message, string confirmText, bool destructive = false);
}
