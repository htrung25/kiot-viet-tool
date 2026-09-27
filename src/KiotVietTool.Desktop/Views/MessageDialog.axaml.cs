using Avalonia.Controls;
using Avalonia.Interactivity;

namespace KiotVietTool.Desktop.Views;

public enum MessageDialogKind { Info, Error, Confirm }

/// <summary>Avalonia has no MessageBox; use through <see cref="Services.IDialogService"/>.</summary>
public sealed partial class MessageDialog : Window
{
    public MessageDialog() => InitializeComponent();

    public MessageDialog(string message, string title, MessageDialogKind kind) : this()
    {
        Title = title;
        MessageText.Text = message;
        HeadingText.Text = kind switch
        {
            MessageDialogKind.Error => "Lỗi",
            MessageDialogKind.Confirm => "Xác nhận",
            _ => "Thông báo",
        };
        HeadingText.Classes.Set("error", kind == MessageDialogKind.Error);
        if (kind == MessageDialogKind.Confirm)
        {
            OkButton.Content = "Có";
            CancelButton.IsVisible = true;
        }
    }

    public bool Result { get; private set; }

    void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Result = true;
        Close(true);
    }

    void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
