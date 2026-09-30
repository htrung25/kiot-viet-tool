namespace KiotVietTool.Desktop.Models;

public sealed record PageLink(int? Number, bool IsCurrent)
{
    public bool IsGap => Number is null;

    public static IReadOnlyList<PageLink> Build(int currentPage, int pageCount)
    {
        var pages = new SortedSet<int> { 1, pageCount, currentPage - 1, currentPage, currentPage + 1 };
        if (currentPage <= 4) pages.UnionWith([2, 3, 4, 5]);
        if (currentPage >= pageCount - 3) pages.UnionWith([pageCount - 4, pageCount - 3, pageCount - 2, pageCount - 1]);

        var links = new List<PageLink>();
        var previous = 0;
        foreach (var page in pages.Where(p => p >= 1 && p <= pageCount))
        {
            if (page - previous == 2) links.Add(new PageLink(previous + 1, false));
            else if (page - previous > 2) links.Add(new PageLink(null, false));
            links.Add(new PageLink(page, page == currentPage));
            previous = page;
        }
        return links;
    }
}
