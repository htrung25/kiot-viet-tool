using Avalonia.Controls;
using Avalonia.Input;
using KiotVietTool.Desktop.ViewModels;

namespace KiotVietTool.Desktop.Views;

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
