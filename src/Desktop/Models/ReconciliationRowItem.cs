using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Desktop.Models;

public sealed record ReconciliationRowItem(InvoiceReconciliationDto Invoice)
{
    public long InvoiceId => Invoice.InvoiceId;
    public string Code => Invoice.Code;
    public string TimeText => VietnamTime.FromUtc(Invoice.PurchasedAtUtc).ToString("HH:mm");
    public string SoldBy => Invoice.SoldByName ?? "—";
    public string TotalText => DisplayFormat.Money(Invoice.Total);
    public string ActualText => DisplayFormat.Money(Invoice.ActualDiscount);
    public string ExpectedText => DisplayFormat.Money(Invoice.ExpectedDiscount);
    public string DifferenceText => Invoice.ActualDiscount == Invoice.ExpectedDiscount ? ""
        : (Invoice.ActualDiscount > Invoice.ExpectedDiscount ? "+" : "−")
            + DisplayFormat.Money(Math.Abs(Invoice.ActualDiscount - Invoice.ExpectedDiscount));
    public string OutcomeText => DisplayFormat.Outcome(Invoice.Outcome);
    public bool NeedsReview => Invoice.NeedsReview && !Invoice.IsReviewed;
    public bool IsMatched => Invoice.Outcome is ReconciliationOutcomeEnum.Matched or ReconciliationOutcomeEnum.NearBoundary;
    public bool IsReviewedProblem => Invoice.NeedsReview && Invoice.IsReviewed;
    public string ProgramNames => Invoice.ProgramNames ?? "—";
    public IReadOnlyList<ReconciledLineItem> Lines { get; } = [.. Invoice.Lines.Select(l => new ReconciledLineItem(l))];
}
