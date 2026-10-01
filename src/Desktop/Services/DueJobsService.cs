using Avalonia.Threading;

using KiotVietTool.Application.Interfaces;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Desktop.Services;

public sealed class DueJobsService(IPriceDeploymentService deployment, INotificationService notifications, ILogger<DueJobsService> logger)
{
    static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    public void Start()
    {
        var timer = new DispatcherTimer { Interval = Interval };
        timer.Tick += (_, _) => RunNow();
        timer.Start();
        RunNow();
    }

    public async void RunNow()
    {
        try
        {
            var handled = await deployment.RunDueAsync();
            if (handled > 0)
                notifications.ShowSuccess($"Đã tự xử lý {handled} chương trình giảm giá tới giờ bắt đầu / kết thúc.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Due price jobs failed");
        }
    }
}
