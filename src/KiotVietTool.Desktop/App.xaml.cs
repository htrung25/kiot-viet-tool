using System.Windows;
using System.Windows.Threading;
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
public sealed partial class App
{
    const string InstanceId = "KiotVietTool";

    SingleInstance? _singleInstance;
    IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = SingleInstance.TryAcquire(InstanceId);
        if (_singleInstance is null)
        {
            Shutdown(); // the running instance was asked to show itself
            return;
        }

        RegisterGlobalExceptionHandlers();
        try
        {
            _host = CreateHost(e.Args);
            await _host.StartAsync();
            await _host.Services.MigrateDatabaseAsync();

            var window = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();
            _singleInstance.OnActivationRequested(() => Dispatcher.BeginInvoke(BringMainWindowToFront));

            await _host.Services.GetRequiredService<INavigationService>().NavigateToAsync<CustomerListViewModel>();
            Log.Information("Application started, version {Version}", typeof(App).Assembly.GetName().Version);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            MessageBox.Show($"Không khởi động được ứng dụng.\n\n{ex.Message}", InstanceId,
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application exiting with code {ExitCode}", e.ApplicationExitCode);
        _host?.Dispose();
        _singleInstance?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
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

                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
                services.AddTransient<CustomerListViewModel>();
                services.AddTransient<CustomerEditViewModel>();
            })
            .Build();

    void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            Log.Error(e.Exception, "Unhandled UI exception");
            ShowUnexpectedError(e.Exception);
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled domain exception (terminating: {IsTerminating})", e.IsTerminating);
            Log.CloseAndFlush();
            ShowUnexpectedError(e.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
            Dispatcher.BeginInvoke(() => ShowUnexpectedError(e.Exception.InnerException ?? e.Exception));
        };
    }

    static void ShowUnexpectedError(Exception? ex) =>
        MessageBox.Show(
            "Đã xảy ra lỗi không mong muốn. Chi tiết đã được ghi vào file log.\n\n" + ex?.Message,
            InstanceId, MessageBoxButton.OK, MessageBoxImage.Error);

    void BringMainWindowToFront()
    {
        if (MainWindow is not { } window) return;
        if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;
        window.Show();
        window.Activate();
        // Topmost toggle forces z-order on top even when Windows denies foreground focus.
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();
    }
}
