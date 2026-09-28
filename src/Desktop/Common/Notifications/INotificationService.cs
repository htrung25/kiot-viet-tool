namespace KiotVietTool.Desktop.Common.Notifications;

/// <summary>Non-blocking toast for successful actions. Errors that need attention use IDialogService.</summary>
public interface INotificationService
{
    void ShowSuccess(string message);
}
