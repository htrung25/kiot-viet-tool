using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using KiotVietTool.Application.Interfaces;
using KiotVietTool.Infrastructure.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KiotVietTool.Desktop.Services;

public sealed partial class IdleLockService : ObservableObject
{
    readonly IUserSessionService _session;
    readonly ICatalogSyncService _sync;
    readonly IPriceDeploymentService _deployment;
    readonly TimeProvider _timeProvider;
    readonly ILogger<IdleLockService> _logger;
    readonly TimeSpan _lockAfter;
    readonly TimeSpan _warnBefore;
    DateTimeOffset _lastActivity;

    public IdleLockService(IOptions<AuthOptions> options, IUserSessionService session, ICatalogSyncService sync,
        IPriceDeploymentService deployment, TimeProvider timeProvider, ILogger<IdleLockService> logger)
    {
        _session = session;
        _sync = sync;
        _deployment = deployment;
        _timeProvider = timeProvider;
        _logger = logger;
        _lockAfter = TimeSpan.FromMinutes(options.Value.IdleLockMinutes);
        _warnBefore = TimeSpan.FromSeconds(options.Value.IdleWarningSeconds);
        _lastActivity = timeProvider.GetUtcNow();
        _session.Changed += (_, _) => Reset();
    }

    public bool IsEnabled => _lockAfter > TimeSpan.Zero;
    public int LockAfterMinutes => (int)_lockAfter.TotalMinutes;

    [ObservableProperty] public partial bool IsLocked { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWarning), nameof(WarningText))]
    public partial int? SecondsUntilLock { get; private set; }

    public bool IsWarning => SecondsUntilLock is not null;
    public string WarningText => $"Tool sẽ tự khoá sau {SecondsUntilLock} giây do không có thao tác. Di chuột hoặc bấm phím để tiếp tục làm việc.";

    public void Attach(InputElement root)
    {
        if (!IsEnabled) return;
        const RoutingStrategies tunnel = RoutingStrategies.Tunnel;
        root.AddHandler(InputElement.KeyDownEvent, (_, _) => RegisterActivity(), tunnel, handledEventsToo: true);
        root.AddHandler(InputElement.PointerPressedEvent, (_, _) => RegisterActivity(), tunnel, handledEventsToo: true);
        root.AddHandler(InputElement.PointerMovedEvent, (_, _) => RegisterActivity(), tunnel, handledEventsToo: true);
        root.AddHandler(InputElement.PointerWheelChangedEvent, (_, _) => RegisterActivity(), tunnel, handledEventsToo: true);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => Check();
        timer.Start();
    }

    public void RegisterActivity()
    {
        if (IsLocked) return;
        _lastActivity = _timeProvider.GetUtcNow();
        SecondsUntilLock = null;
    }

    public void Check()
    {
        if (!IsEnabled || IsLocked || _session.CurrentUser is not { MustChangePassword: false })
        {
            SecondsUntilLock = null;
            return;
        }
        if (_sync.IsRunning || _deployment.IsRunning)
        {
            RegisterActivity();
            return;
        }

        var remaining = _lockAfter - (_timeProvider.GetUtcNow() - _lastActivity);
        if (remaining <= TimeSpan.Zero)
        {
            SecondsUntilLock = null;
            IsLocked = true;
            _logger.LogInformation("Session locked after {Minutes} minutes without activity", LockAfterMinutes);
            return;
        }
        SecondsUntilLock = remaining <= _warnBefore ? (int)Math.Ceiling(remaining.TotalSeconds) : null;
    }

    public void Unlock()
    {
        IsLocked = false;
        RegisterActivity();
    }

    void Reset()
    {
        IsLocked = false;
        _lastActivity = _timeProvider.GetUtcNow();
        SecondsUntilLock = null;
    }
}
