using Avalonia.Threading;

using KiotVietTool.Application.Interfaces;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Desktop.Services;

public sealed class DiscountFeedSyncService(IDiscountFeedService feed, IKiotVietApiService api, ILogger<DiscountFeedSyncService> logger)
{
    static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    public void Start()
    {
        var timer = new DispatcherTimer { Interval = Interval };
        timer.Tick += (_, _) => RunNow();
        timer.Start();
        RunNow();
    }

    async void RunNow()
    {
        try
        {
            await api.SyncClockAsync(CancellationToken.None);
            await feed.SyncAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Discount feed sync failed");
        }
    }
}
