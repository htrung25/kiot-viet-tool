namespace KiotVietTool.Application.Common.Pagination;

public sealed record PagedResult<T>
{
    PagedResult(IReadOnlyList<T> items, int page, int pageSize, int? totalItems, bool hasNext)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
        HasNext = hasNext;
    }

    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int? TotalItems { get; }
    public bool HasNext { get; }

    public int? TotalPages => TotalItems is { } total ? Math.Max(1, (total + PageSize - 1) / PageSize) : null;
    public bool HasPrevious => Page > 1;
    public int From => Items.Count == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int To => Items.Count == 0 ? 0 : From + Items.Count - 1;

    public static PagedResult<T> Create(IReadOnlyList<T> items, PageRequest request, int totalItems) =>
        new(items, request.Page, request.PageSize, totalItems, request.Page * request.PageSize < totalItems);

    public static PagedResult<T> CreateWithoutTotal(IReadOnlyList<T> itemsPlusOne, PageRequest request)
    {
        var hasNext = itemsPlusOne.Count > request.PageSize;
        var items = hasNext ? [.. itemsPlusOne.Take(request.PageSize)] : itemsPlusOne;
        return new(items, request.Page, request.PageSize, null, hasNext);
    }

    public static PagedResult<T> Empty(PageRequest request) =>
        new([], request.Page, request.PageSize, request.IncludeTotal ? 0 : null, false);

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> map) =>
        new([.. Items.Select(map)], Page, PageSize, TotalItems, HasNext);
}
