namespace KiotVietTool.Infrastructure.Options;

public sealed class DiscountFeedOptions
{
    public const string SectionName = "DiscountFeed";

    public int RequestTimeoutSeconds { get; set; } = 20;
}
