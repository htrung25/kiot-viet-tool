using System.Globalization;

using KiotVietTool.Application.Common;
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

    public static string Status(ProgramStatusEnum status) => status switch
    {
        ProgramStatusEnum.Draft => "Nháp",
        ProgramStatusEnum.Scheduled => "Đã lên lịch",
        ProgramStatusEnum.Applying => "Đang áp giá",
        ProgramStatusEnum.Running => "Đang chạy",
        ProgramStatusEnum.ApplyFailed => "Lỗi áp giá",
        ProgramStatusEnum.Restoring => "Đang trả giá",
        ProgramStatusEnum.RestoreFailed => "Lỗi trả giá",
        ProgramStatusEnum.Ended => "Đã kết thúc",
        ProgramStatusEnum.Stopped => "Đã dừng",
        _ => status.ToString(),
    };
}
