namespace KiotVietTool.Application.DTOs;

public sealed record KiotVietPageDto<T>(IReadOnlyList<T> Items, int Total, IReadOnlyList<long> RemovedIds);
