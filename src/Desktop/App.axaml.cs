using System.Runtime.InteropServices;

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using KiotVietTool.Application;
using KiotVietTool.Desktop.Services;
using KiotVietTool.Desktop.ViewModels;
using KiotVietTool.Desktop.Views;
using KiotVietTool.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace KiotVietTool.Desktop;

/// <summary>Composition root.</summary>
public sealed partial class App(SingleInstance? singleInstance) : Avalonia.Application
{
    const string UnexpectedErrorMessage = "Đã xảy ra lỗi không mong muốn. Chi tiết đã được ghi vào file log.";

    IHost? _host;
    IClassicDesktopStyleApplicationLifetime? _desktop;

    /// <summary>Used by the XAML previewer.</summary>
    public App() : this(null) { }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop;
            RegisterGlobalExceptionHandlers();

            _host = CreateHost(desktop.Args ?? []);
            var window = _host.Services.GetRequiredService<MainWindow>();
            window.Opened += OnMainWindowOpened;
            desktop.MainWindow = window;
            desktop.Exit += OnExit;

            singleInstance?.ListenForActivation(() => Dispatcher.UIThread.Post(BringMainWindowToFront));
        }

        base.OnFrameworkInitializationCompleted();
    }

    async void OnMainWindowOpened(object? sender, EventArgs e)
    {
        ((Window)sender!).Opened -= OnMainWindowOpened;
        var services = _host!.Services;
        try
        {
            await _host.StartAsync();
            await services.InitializeDatabaseAsync();
            await services.GetRequiredService<INavigationService>().NavigateToAsync<LoginViewModel>();
            Log.Information("Application started, version {Version}", typeof(App).Assembly.GetName().Version);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            await services.GetRequiredService<IDialogService>()
                .ShowErrorAsync($"Không khởi động được ứng dụng.\n\n{ex.Message}");
            _desktop!.Shutdown(1);
        }
    }

    void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        Log.Information("Application exiting with code {ExitCode}", e.ApplicationExitCode);
        _host?.Dispose();
        Log.CloseAndFlush();
    }

    static IHost CreateHost(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseContentRoot(AppContext.BaseDirectory) // single-file: appsettings.json sits next to the .exe
            .UseSerilog((context, services, logger) => logger
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext())
            .ConfigureServices((context, services) =>
            {
                services.AddApplication();
                services.AddInfrastructure(context.Configuration);

                services.AddSingleton(new NavigationRoutes(
                    Login: typeof(LoginViewModel),
                    ChangePassword: typeof(ChangePasswordViewModel),
                    Home: typeof(ProductListViewModel)));
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<INotificationService>(sp => new NotificationService(sp.GetRequiredService<MainWindow>()));

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<ChangePasswordViewModel>();
                services.AddTransient<ProductListViewModel>();
            })
            .Build();

    void RegisterGlobalExceptionHandlers()
    {
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            Log.Error(e.Exception, "Unhandled UI exception");
            e.Handled = true;
            ShowUnexpectedError(e.Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
            Dispatcher.UIThread.Post(() => ShowUnexpectedError(e.Exception.InnerException ?? e.Exception));
        };

        // Process is terminating: no UI loop left, so use a blocking native message box (Windows only).
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled domain exception (terminating: {IsTerminating})", e.IsTerminating);
            Log.CloseAndFlush();
            if (OperatingSystem.IsWindows())
                NativeMessageBox(IntPtr.Zero, $"{UnexpectedErrorMessage}\n\n{(e.ExceptionObject as Exception)?.Message}",
                    "KiotVietTool", MbIconError);
        };
    }

    async void ShowUnexpectedError(Exception ex)
    {
        if (_host?.Services.GetService<IDialogService>() is not { } dialog) return;
        try { await dialog.ShowErrorAsync($"{UnexpectedErrorMessage}\n\n{ex.Message}"); }
        catch (Exception dialogEx) { Log.Error(dialogEx, "Could not show error dialog"); }
    }

    void BringMainWindowToFront()
    {
        if (_desktop?.MainWindow is not { } window) return;
        Log.Information("Another launch was blocked; bringing main window to front");
        if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
        window.Show();
        window.Activate();
        // Topmost toggle forces z-order on top even when the OS denies foreground focus.
        window.Topmost = true;
        window.Topmost = false;
    }

    const uint MbIconError = 0x10;

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    static extern int NativeMessageBox(IntPtr hWnd, string text, string caption, uint type);
}
