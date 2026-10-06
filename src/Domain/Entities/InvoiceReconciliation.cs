using System.Text.Json;

using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Domain.Entities;

public sealed class InvoiceReconciliation
{
    public const decimal Tolerance = 1; // KiotViet rounds the invoice discount to whole đồng
    public static readonly TimeSpan ClockMargin = TimeSpan.FromMinutes(2); // extension clock vs KiotViet purchase time
    public static readonly TimeSpan FeedPropagation = TimeSpan.FromMinutes(3); // KV propagation + 1-minute extension refresh

    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public long InvoiceId { get; private set; }
    public string Code { get; private set; } = "";
    public DateTime PurchasedAtUtc { get; private set; }
    public string? BranchName { get; private set; }
    public string? SoldByName { get; private set; }
    public decimal Total { get; private set; }
    public decimal ActualDiscount { get; private set; }
    public decimal ExpectedDiscount { get; private set; }
    public ReconciliationOutcomeEnum Outcome { get; private set; }
    public string? ProgramNames { get; private set; }
    public string LinesJson { get; private set; } = "[]";
    public bool IsReviewed { get; private set; }
    public DateTime ReconciledAtUtc { get; private set; }

    private InvoiceReconciliation() { } // EF Core

    public bool NeedsReview => Outcome is ReconciliationOutcomeEnum.MissingDiscount or ReconciliationOutcomeEnum.WrongAmount
        or ReconciliationOutcomeEnum.UnexpectedDiscount;

    public IReadOnlyList<ReconciledLine> Lines => JsonSerializer.Deserialize<List<ReconciledLine>>(LinesJson, Json) ?? [];

    // snapshots: feeds a cashier may have held at purchase time, oldest first; the last one is the feed in effect.
    public static InvoiceReconciliation Create(long invoiceId, string code, DateTime purchasedAtUtc, string? branchName,
        string? soldByName, decimal total, decimal actualDiscount, IReadOnlyList<CheckoutLine> lines,
        IReadOnlyList<DiscountFeedSnapshot> snapshots, DateTime nowUtc)
    {
        if (snapshots.Count == 0) throw new ArgumentException("The feed in effect is required.", nameof(snapshots));

        var (expected, reconciledLines) = snapshots[^1].Evaluate(lines, purchasedAtUtc);
        // Only an older feed or a shifted clock explains the amount: timing, not an error.
        var alternatives = snapshots.SelectMany(s => new[] { purchasedAtUtc - ClockMargin, purchasedAtUtc, purchasedAtUtc + ClockMargin }
            .Select(at => s.Evaluate(lines, at).Amount));

        var outcome = Math.Abs(actualDiscount - expected) <= Tolerance ? ReconciliationOutcomeEnum.Matched
            : alternatives.Any(a => Math.Abs(actualDiscount - a) <= Tolerance) ? ReconciliationOutcomeEnum.NearBoundary
            : actualDiscount <= 0 ? ReconciliationOutcomeEnum.MissingDiscount
            : expected <= 0 ? ReconciliationOutcomeEnum.UnexpectedDiscount
            : ReconciliationOutcomeEnum.WrongAmount;

        var programs = string.Join(", ", reconciledLines.Select(l => l.ProgramName).OfType<string>().Distinct());
        return new InvoiceReconciliation
        {
            InvoiceId = invoiceId,
            Code = code,
            PurchasedAtUtc = purchasedAtUtc,
            BranchName = branchName,
            SoldByName = soldByName,
            Total = total,
            ActualDiscount = actualDiscount,
            ExpectedDiscount = expected,
            Outcome = outcome,
            ProgramNames = programs.Length > 0 ? programs : null,
            LinesJson = JsonSerializer.Serialize(reconciledLines, Json),
            ReconciledAtUtc = nowUtc,
        };
    }

    // A re-check keeps the review mark unless the outcome changed.
    public void ReplaceWith(InvoiceReconciliation recheck)
    {
        if (recheck.Outcome != Outcome) IsReviewed = false;
        Code = recheck.Code;
        PurchasedAtUtc = recheck.PurchasedAtUtc;
        BranchName = recheck.BranchName;
        SoldByName = recheck.SoldByName;
        Total = recheck.Total;
        ActualDiscount = recheck.ActualDiscount;
        ExpectedDiscount = recheck.ExpectedDiscount;
        Outcome = recheck.Outcome;
        ProgramNames = recheck.ProgramNames;
        LinesJson = recheck.LinesJson;
        ReconciledAtUtc = recheck.ReconciledAtUtc;
    }

    public void MarkReviewed() => IsReviewed = true;
}
