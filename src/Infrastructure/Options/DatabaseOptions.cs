namespace KiotVietTool.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>SQLite file path; environment variables like %LOCALAPPDATA% are expanded.</summary>
    public string Path { get; set; } = "";

    public string ResolvedPath => Environment.ExpandEnvironmentVariables(Path);
}
