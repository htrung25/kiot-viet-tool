using Avalonia.Controls;

namespace KiotVietTool.Desktop.Views;

public sealed partial class ChangePasswordView : UserControl
{
    public ChangePasswordView()
    {
        InitializeComponent();
        Loaded += (_, _) => CurrentPasswordBox.Focus();
    }
}
