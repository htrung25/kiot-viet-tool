namespace KiotVietTool.Application.Common.Pagination;

public sealed record SortRequest<TField>(TField Field, SortDirection Direction = SortDirection.Ascending)
    where TField : struct, Enum;
