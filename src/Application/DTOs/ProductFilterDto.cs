using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Application.DTOs;

public sealed record ProductFilterDto(string? Keyword = null, int? CategoryId = null, ProductEnum? Type = null, bool? IsActive = null);
