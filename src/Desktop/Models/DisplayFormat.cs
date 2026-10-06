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
            "Chưa kết nối máy thu ngân (Hệ thống → Máy thu ngân), nên chưa áp dụng được chương trình.",
        { Conflict: not null } =>
            "Tool tạm ngừng gửi danh sách giảm giá: Worker đang giữ dữ liệu do máy khác (hoặc bản dữ liệu khác của tool) gửi. Vào Hệ thống → Máy thu ngân để xử lý.",
        { LastError: { } error } =>
            $"Chưa gửi được danh sách giảm giá mới nhất tới máy thu ngân: {error} Tool tự gửi lại mỗi phút.",
        _ => null,
    };

    public static string FeedConflict(DiscountFeedConflictDto conflict) => conflict.ByOtherInstallation
        ? $"Worker đang giữ danh sách giảm giá do một máy khác gửi lúc {DateTime(conflict.WrittenAtUtc)}. Tool trên máy này tạm ngừng gửi để không ghi đè. "
            + "Nếu máy này là máy quản lý chính, bấm Ghi đè bằng máy này; nếu không, hãy quản lý chương trình trên máy kia."
        : $"Worker đang giữ danh sách giảm giá mới hơn dữ liệu trên máy này (gửi lúc {DateTime(conflict.WrittenAtUtc)}), có thể do dữ liệu tool vừa được khôi phục từ bản sao lưu. "
            + "Tool tạm ngừng gửi để không ghi đè. Kiểm tra lại các chương trình rồi bấm Ghi đè bằng máy này.";
}
