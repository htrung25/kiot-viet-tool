using System.Globalization;

using KiotVietTool.Application.Common;
using KiotVietTool.Application.Enums;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Desktop.Models;

public static class DisplayFormat
{
    public static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string Money(decimal amount) => amount.ToString("#,##0", Vietnamese) + " ₫";

    public static string Number(int value) => value.ToString("N0", Vietnamese);

    public static string DateTime(DateTime? utc) =>
        utc is { } value ? VietnamTime.FromUtc(value).ToString("dd/MM/yyyy HH:mm", Vietnamese) : "—";

    public static string DiscountValue(DiscountEnum type, decimal value) =>
        type == DiscountEnum.Percent ? value.ToString("0.##", Vietnamese) + "%" : Money(value);

    public static string Status(ProgramDisplayStatusEnum status) => status switch
    {
        ProgramDisplayStatusEnum.Draft => "Nháp",
        ProgramDisplayStatusEnum.Deploying => "Đang triển khai",
        ProgramDisplayStatusEnum.Scheduled => "Đã lên lịch",
        ProgramDisplayStatusEnum.Running => "Đang chạy",
        ProgramDisplayStatusEnum.Ended => "Đã kết thúc",
        ProgramDisplayStatusEnum.NeedsRedeploy => "Cần triển khai lại",
        ProgramDisplayStatusEnum.DeployFailed => "Lỗi triển khai",
        ProgramDisplayStatusEnum.StopFailed => "Đang dừng — lỗi",
        ProgramDisplayStatusEnum.Cancelled => "Đã huỷ",
        _ => status.ToString(),
    };
}
