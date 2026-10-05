namespace KiotVietTool.Infrastructure.Options;

public sealed class DiscountFeedOptions
{
    public const string SectionName = "DiscountFeed";

    public string Url { get; set; } = "";
    public string WriteToken { get; set; } = "";
    public int RequestTimeoutSeconds { get; set; } = 20;
}
