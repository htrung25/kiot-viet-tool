using KiotVietTool.Application.Interfaces;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class ServerClockService(TimeProvider machineClock, ILogger<ServerClockService> logger) : TimeProvider, IServerClockService
{
    static readonly TimeSpan SkewWarningThreshold = TimeSpan.FromMinutes(2);
    static readonly TimeSpan MinChange = TimeSpan.FromSeconds(2);
    static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(10);
    static readonly TimeSpan DateHeaderRounding = TimeSpan.FromMilliseconds(500);

    readonly Lock _lock = new();
    TimeSpan _offset;
    DateTimeOffset? _observedAt;

    public TimeSpan Offset
    {
        get { lock (_lock) return _offset; }
    }

    public bool IsSkewed => Offset.Duration() >= SkewWarningThreshold;

    public bool IsStale
    {
        get { lock (_lock) return _observedAt is not { } at || machineClock.GetUtcNow() - at > StaleAfter; }
    }

    public event EventHandler? OffsetChanged;

    public override DateTimeOffset GetUtcNow() => machineClock.GetUtcNow() + Offset;

    public void Observe(DateTimeOffset serverTime)
    {
        var machineNow = machineClock.GetUtcNow();
        var offset = serverTime + DateHeaderRounding - machineNow;
        TimeSpan previous;
        lock (_lock)
        {
            _observedAt = machineNow;
            previous = _offset;
            if ((offset - previous).Duration() < MinChange) return;
            _offset = offset;
        }

        var level = offset.Duration() >= SkewWarningThreshold ? LogLevel.Warning : LogLevel.Information;
        logger.Log(level, "Machine clock offset to KiotViet changed from {Previous} to {Offset} (positive = machine is behind)",
            previous, offset);
        OffsetChanged?.Invoke(this, EventArgs.Empty);
    }

    public DateTime ToMachineUtc(DateTime serverUtc) => serverUtc - Offset;
}
