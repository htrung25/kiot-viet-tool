using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace KiotVietTool.Desktop.Common.Dialogs;

/// <summary>Avalonia has no MessageBox; use through <see cref="IDialogService"/>.</summary>
public sealed partial class MessageDialog : Window
{
    /// <summary>Used by the XAML previewer.</summary>
    public MessageDialog() => InitializeComponent();

    public MessageDialog(string heading, string message, MessageDialogKind kind, string confirmText) : this()
    {
        HeadingText.Text = heading;
        MessageText.Text = message;
        OkButton.Content = confirmText;

        var isConfirm = kind is MessageDialogKind.Confirm or MessageDialogKind.DestructiveConfirm;
        var isDanger = kind is MessageDialogKind.Error or MessageDialogKind.DestructiveConfirm;
        CancelButton.IsVisible = isConfirm;
        OkButton.IsCancel = !isConfirm; // Esc closes a plain message
        OkButton.Classes.Set("danger", kind == MessageDialogKind.DestructiveConfirm);
        OkButton.Classes.Set("accent", kind != MessageDialogKind.DestructiveConfirm);
        IconBadge.Classes.Set("danger", isDanger);
        KindIcon.Data = (Geometry)this.FindResource(kind switch
        {
            MessageDialogKind.Error or MessageDialogKind.DestructiveConfirm => "Icon.AlertCircle",
            MessageDialogKind.Confirm => "Icon.Help",
            _ => "Icon.Info",
        })!;
    }

    public bool Result { get; private set; }

    void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Result = true;
        Close(true);
    }

    void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
