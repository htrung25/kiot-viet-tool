using Avalonia.Threading;

using KiotVietTool.Application.Interfaces;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Desktop.Services;

public sealed class ReconciliationSyncService(IInvoiceReconciliationService reconciliation, ILogger<ReconciliationSyncService> logger)
{
    static readonly TimeSpan FirstRunDelay = TimeSpan.FromMinutes(1);
    static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    public void Start()
    {
        var timer = new DispatcherTimer { Interval = FirstRunDelay };
        timer.Tick += (_, _) =>
        {
            timer.Interval = Interval;
            RunNow();
        };
        timer.Start();
    }

    async void RunNow()
    {
        try
        {
            // Off the UI thread: a day of invoices is evaluated against every feed snapshot.
            var result = await Task.Run(() => reconciliation.RunAsync());
            if (!result.IsSuccess) logger.LogInformation("Scheduled reconciliation skipped: {Reason}", result.Error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scheduled reconciliation failed");
        }
    }
}
