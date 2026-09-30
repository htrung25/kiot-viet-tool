namespace KiotVietTool.Application.Common;

public static class VietnamTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    public static DateTime ToUtc(DateTime vietnamLocal) =>
        DateTime.SpecifyKind(vietnamLocal - Offset, DateTimeKind.Utc);

    public static DateTime FromUtc(DateTime utc) =>
        DateTime.SpecifyKind(utc + Offset, DateTimeKind.Unspecified);
}
