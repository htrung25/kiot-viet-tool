namespace KiotVietTool.Application.Interfaces;

public interface IServerClockService
{
    TimeSpan Offset { get; }

    bool IsSkewed { get; }

    bool IsStale { get; }

    event EventHandler? OffsetChanged;

    void Observe(DateTimeOffset serverTime);

    DateTime ToMachineUtc(DateTime serverUtc);
}
