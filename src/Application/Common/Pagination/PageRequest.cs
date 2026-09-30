namespace KiotVietTool.Application.Common.Pagination;

public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxPage = 100_000;

    public int Page { get; init => field = Math.Clamp(value, 1, MaxPage); } = 1;
    public int PageSize { get; init => field = Math.Clamp(value, 1, MaxPageSize); } = DefaultPageSize;
    public bool IncludeTotal { get; init; } = true;

    public int Skip => (Page - 1) * PageSize;
}
