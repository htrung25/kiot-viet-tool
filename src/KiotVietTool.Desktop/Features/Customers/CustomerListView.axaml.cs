using Avalonia.Controls;
using Avalonia.Input;

namespace KiotVietTool.Desktop.Features.Customers;

public sealed partial class CustomerListView : UserControl
{
    public CustomerListView() => InitializeComponent();

    // Avalonia has no MouseBinding for double-click; forward to the command.
    void Grid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is CustomerListViewModel vm && vm.EditCommand.CanExecute(null))
            vm.EditCommand.Execute(null);
    }
}
