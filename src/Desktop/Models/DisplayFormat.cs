using System.Globalization;

using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
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

    public static string Phase(ProgramPhaseEnum phase) => phase switch
    {
        ProgramPhaseEnum.Draft => "Nháp",
        ProgramPhaseEnum.Upcoming => "Sắp diễn ra",
        ProgramPhaseEnum.Live => "Đang chạy",
        ProgramPhaseEnum.Ended => "Đã kết thúc",
        ProgramPhaseEnum.Stopped => "Đã dừng",
        _ => phase.ToString(),
    };

    public static string? FeedWarning(DiscountFeedStatusDto status) => status switch
    {
        { IsConfigured: false } =>
            "Chưa cấu hình kết nối tới máy thu ngân (mục DiscountFeed trong appsettings.json), nên chưa áp dụng được chương trình.",
        { LastError: { } error } =>
            $"Chưa gửi được danh sách giảm giá mới nhất tới máy thu ngân: {error} Tool tự gửi lại mỗi phút.",
        _ => null,
    };
}
