namespace KiotVietTool.Infrastructure.Options;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string ApiBaseUrl { get; set; } = "";
    public int RequestTimeoutSeconds { get; set; } = 20;
    public string ProxyUrl { get; set; } = "";
}
