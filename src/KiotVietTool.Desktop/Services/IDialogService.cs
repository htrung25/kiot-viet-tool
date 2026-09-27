namespace KiotVietTool.Desktop.Services;

public interface IDialogService
{
    Task ShowInfoAsync(string message);
    Task ShowErrorAsync(string message);
    Task<bool> ConfirmAsync(string message);
}
