using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Application.DTOs;

public sealed record InvoiceReconciliationDto(long InvoiceId, string Code, DateTime PurchasedAtUtc, string? BranchName,
    string? SoldByName, decimal Total, decimal ActualDiscount, decimal ExpectedDiscount, ReconciliationOutcomeEnum Outcome,
    string? ProgramNames, bool NeedsReview, bool IsReviewed, IReadOnlyList<ReconciledLine> Lines);
