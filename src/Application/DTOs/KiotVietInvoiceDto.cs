using KiotVietTool.Domain.Entities;

namespace KiotVietTool.Application.DTOs;

public sealed record KiotVietInvoiceDto(long Id, string Code, DateTime PurchasedAtUtc, string? BranchName, string? SoldByName,
    decimal Total, decimal Discount, bool IsCompleted, IReadOnlyList<CheckoutLine> Lines);
