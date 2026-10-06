using Avalonia.Controls;

using KiotVietTool.Desktop.ViewModels;

namespace KiotVietTool.Desktop.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    public MainWindow(MainViewModel viewModel) : this() => DataContext = viewModel;
}
