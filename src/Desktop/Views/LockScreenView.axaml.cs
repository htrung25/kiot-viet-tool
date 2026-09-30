using Avalonia.Controls;

namespace KiotVietTool.Desktop.Views;

public sealed partial class LockScreenView : UserControl
{
    public LockScreenView()
    {
        InitializeComponent();
        Loaded += (_, _) => PasswordBox.Focus();
    }
}
