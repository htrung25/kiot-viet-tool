namespace KiotVietTool.Infrastructure.Options;

public sealed class CloudflareOptions
{
    public const string SectionName = "Cloudflare";

    public string ApiBaseUrl { get; set; } = "";
    public int RequestTimeoutSeconds { get; set; } = 60;

    public string CompatibilityDate { get; set; } = "";
}
