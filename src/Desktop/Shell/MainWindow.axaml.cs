using Avalonia.Controls;

namespace KiotVietTool.Desktop.Shell;

public sealed partial class MainWindow : Window
{
    /// <summary>Used by the XAML previewer.</summary>
    public MainWindow() => InitializeComponent();

    public MainWindow(MainViewModel viewModel) : this() => DataContext = viewModel;
}
