namespace KiotVietTool.Application.Common.Pagination;

public record PageRequest
{
    private const int MaxPageSize = 100;

    public int Page {get; init;} = 1;
    public int PageSize {get; init;} = 20;
    public string? SortBy { get; init;}
    public bool Desc { get; init;} = true;
    public string? Search { get; init; }
    public bool IncludeTotal { get; init;} = true ;

    public int SafePage => Page < 1 ? 1 :Page;
    public int SafeSize => Math.Clamp(PageSize, 1, MaxPageSize);

}