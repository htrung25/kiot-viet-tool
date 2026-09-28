using Avalonia.Controls;

namespace KiotVietTool.Desktop.Views;

public sealed partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        Loaded += (_, _) => UsernameBox.Focus();
    }
}
