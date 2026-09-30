namespace KiotVietTool.Application.Common.Pagination;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long? TotalItems { get; init; }
    public int? TotalPages { get; init; }
    public bool HasPrevious => Page > 1;
    public bool HasNext { get; init; }

    // "Hiển thị 11–20 / 245"
    public long From => Items.Count == 0 ? 0 : (long)(Page - 1) * PageSize + 1;
    public long To => From == 0 ? 0 : From + Items.Count - 1;

    // [1, null, 4, 5, 6, null, 20]  -> null = "…"
    public IReadOnlyList<int?> PageNumbers { get; init; } = [];
}