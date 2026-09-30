using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace KiotVietTool.Desktop.Services;

/// <summary>
/// Named mutex = "already running?"; named pipe = "second launch asks the first to show itself".
/// Both work on Windows and macOS.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    readonly Mutex _mutex;
    readonly string _pipeName;
    readonly CancellationTokenSource _cts = new();

    SingleInstance(Mutex mutex, string pipeName)
    {
        _mutex = mutex;
        _pipeName = pipeName;
    }

    /// <summary>Returns null if another instance is running (after signalling it to activate).</summary>
    public static SingleInstance? TryAcquire(string id)
    {
        var name = $"{id}.{Environment.UserName}";
        // Windows "Local\" = per logon session. On Unix "Local\" is per process session (each launch differs),
        // so use "Global\" there; the user name in `name` keeps it per user.
        var scope = OperatingSystem.IsWindows() ? "Local" : "Global";
        var mutex = new Mutex(initiallyOwned: true, $@"{scope}\{name}", out var createdNew);
        if (createdNew) return new SingleInstance(mutex, name);

        mutex.Dispose();
        SignalRunningInstance(name);
        return null;
    }

    static void SignalRunningInstance(string pipeName)
    {
        // Let the running instance take foreground focus (Windows blocks it otherwise).
        if (OperatingSystem.IsWindows()) AllowSetForegroundWindow(AsfwAny);
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            client.Connect(TimeSpan.FromSeconds(2));
        }
        catch (Exception e) when (e is TimeoutException or IOException)
        {
            // Running instance is starting up or shutting down; nothing to activate.
        }
    }

    /// <summary>Invokes <paramref name="onActivate"/> (on a background thread) each time another launch is attempted.</summary>
    public void ListenForActivation(Action onActivate) => _ = ListenAsync(onActivate, _cts.Token);

    async Task ListenAsync(Action onActivate, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(cancellationToken);
                onActivate();
            }
            catch (OperationCanceledException) { return; }
            catch (IOException) { return; } // pipe unavailable: app still works, only re-activation is lost
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }

    const int AsfwAny = -1;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool AllowSetForegroundWindow(int processId);
}
