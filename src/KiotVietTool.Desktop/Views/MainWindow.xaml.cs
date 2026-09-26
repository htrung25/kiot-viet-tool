using KiotVietTool.Desktop.ViewModels;

namespace KiotVietTool.Desktop.Views;

public sealed partial class MainWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
