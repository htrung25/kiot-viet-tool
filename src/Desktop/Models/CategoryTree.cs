using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Desktop.Models;

public static class CategoryTree
{
    public static IEnumerable<(CategoryDto Category, int Depth)> Flatten(IReadOnlyList<CategoryDto> categories)
    {
        var ids = categories.Select(c => c.Id).ToHashSet();
        var children = categories.ToLookup(c => c.ParentId is { } p && ids.Contains(p) ? c.ParentId : null);

        IEnumerable<(CategoryDto, int)> Walk(int? parentId, int depth) =>
            children[parentId].SelectMany(c => Walk(c.Id, depth + 1).Prepend((c, depth)));

        return Walk(null, 0);
    }
}
