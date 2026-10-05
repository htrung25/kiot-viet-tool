using Avalonia;

using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop;

internal static class Program
{
    const string InstanceId = "KiotVietTool";

    [STAThread]
    public static int Main(string[] args)
    {
        using var singleInstance = SingleInstanceService.TryAcquire(InstanceId);
        if (singleInstance is null) return 0; // the running instance handles the request

        // %LOCALAPPDATA% only exists on Windows; define it so appsettings.json paths also work in macOS dev.
        if (Environment.GetEnvironmentVariable("LOCALAPPDATA") is null)
            Environment.SetEnvironmentVariable("LOCALAPPDATA",
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

        return Configure(AppBuilder.Configure(() => new App(singleInstance)))
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>Entry point for the XAML previewer; do not remove.</summary>
    public static AppBuilder BuildAvaloniaApp() => Configure(AppBuilder.Configure<App>());

    static AppBuilder Configure(AppBuilder builder) =>
        builder
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
