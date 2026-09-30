using System.Globalization;

using KiotVietTool.Application.Common;

namespace KiotVietTool.Desktop.Models;

public static class DisplayFormat
{
    public static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string Money(decimal amount) => amount.ToString("#,##0", Vietnamese) + " ₫";

    public static string Number(int value) => value.ToString("N0", Vietnamese);

    public static string DateTime(DateTime? utc) =>
        utc is { } value ? VietnamTime.FromUtc(value).ToString("dd/MM/yyyy HH:mm", Vietnamese) : "—";

}
