using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace KiotVietTool.Desktop.Common.Notifications;

public sealed class NotificationService(TopLevel host) : INotificationService
{
    // Created up front: the manager drops notifications shown before its template is applied,
    // so creating it lazily inside ShowSuccess would lose the first toast.
    readonly WindowNotificationManager _manager =
        new(host) { Position = NotificationPosition.BottomRight, MaxItems = 3 };

    public void ShowSuccess(string message) =>
        _manager.Show(new Notification("Thành công", message, NotificationType.Success, TimeSpan.FromSeconds(3)));
}
