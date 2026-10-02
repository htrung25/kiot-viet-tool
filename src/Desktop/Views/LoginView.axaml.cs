using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Threading;

using KiotVietTool.Desktop.ViewModels;

namespace KiotVietTool.Desktop.Views;

public sealed partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        Loaded += (_, _) => UsernameBox.Focus();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is LoginViewModel vm) vm.PropertyChanged += OnViewModelPropertyChanged;
        };
    }

    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginViewModel.IsOtpStep) && sender is LoginViewModel { IsOtpStep: true })
            Dispatcher.UIThread.Post(() => OtpBox.Focus());
    }
}
