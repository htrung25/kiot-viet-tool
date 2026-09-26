using System.Runtime.InteropServices;

namespace KiotVietTool.Desktop;

/// <summary>
/// Named mutex = "already running?"; named event = "second launch asks the first to show itself".
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    readonly Mutex _mutex;
    readonly EventWaitHandle _activateEvent;
    RegisteredWaitHandle? _registration;

    SingleInstance(Mutex mutex, EventWaitHandle activateEvent)
    {
        _mutex = mutex;
        _activateEvent = activateEvent;
    }

    /// <summary>Returns null if another instance owns the mutex (after signalling it to activate).</summary>
    public static SingleInstance? TryAcquire(string id)
    {
        var mutex = new Mutex(initiallyOwned: true, $@"Local\{id}.Mutex", out var createdNew);
        var activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $@"Local\{id}.Activate");
        if (createdNew) return new SingleInstance(mutex, activateEvent);

        // Let the running instance take foreground focus (Windows blocks it otherwise).
        AllowSetForegroundWindow(AsfwAny);
        activateEvent.Set();
        activateEvent.Dispose();
        mutex.Dispose();
        return null;
    }

    public void OnActivationRequested(Action callback) =>
        _registration = ThreadPool.RegisterWaitForSingleObject(
            _activateEvent, (_, _) => callback(), null, Timeout.Infinite, executeOnlyOnce: false);

    public void Dispose()
    {
        _registration?.Unregister(null);
        _activateEvent.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }

    const int AsfwAny = -1;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool AllowSetForegroundWindow(int processId);
}
