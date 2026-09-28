using Avalonia.Controls;

namespace KiotVietTool.Desktop.Features.Customers;

public sealed partial class CustomerEditView : UserControl
{
    public CustomerEditView()
    {
        InitializeComponent();
        Loaded += (_, _) => CodeBox.Focus();
    }
}
