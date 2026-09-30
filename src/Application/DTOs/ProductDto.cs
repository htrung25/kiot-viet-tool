using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Application.DTOs;

public sealed record ProductDto(
    long Id,
    string Code,
    string FullName,
    string? CategoryName,
    string Unit,
    ProductEnum Type,
    decimal BasePrice,
    bool IsActive,
    bool AllowsSale);
