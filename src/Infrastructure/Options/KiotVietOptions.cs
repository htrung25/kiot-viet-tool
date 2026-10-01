namespace KiotVietTool.Infrastructure.Options;

public sealed class KiotVietOptions
{
    public const string SectionName = "KiotViet";

    public string TokenUrl { get; set; } = "";
    public string ApiBaseUrl { get; set; } = "";
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MaxGetRequestsPerHour { get; set; } = 4500;
    public int MaxRetries { get; set; } = 3;
    public int PriceUpdateBatchSize { get; set; } = 20;
    public int MinWriteIntervalMs { get; set; } = 500;
}
