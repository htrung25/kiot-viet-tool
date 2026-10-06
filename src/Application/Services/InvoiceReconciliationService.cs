using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Exceptions;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Domain.Entities;
using KiotVietTool.Domain.Enums;

using Microsoft.Extensions.Logging;

namespace KiotVietTool.Application.Services;

internal sealed class InvoiceReconciliationService(
    IKiotVietConnectionRepository connections,
    IKiotVietApiService api,
    ISecretProtectorService secretProtector,
    IDiscountFeedSnapshotRepository snapshots,
    IInvoiceReconciliationRepository reconciliations,
    ITelegramService telegram,
    TimeProvider timeProvider,
    ILogger<InvoiceReconciliationService> logger) : IInvoiceReconciliationService
{
    static readonly TimeSpan SettleDelay = TimeSpan.FromMinutes(1); // invoices of the last minute may still be saving
    static readonly TimeSpan Overlap = TimeSpan.FromMinutes(5);
    static readonly TimeSpan FirstRunLookback = TimeSpan.FromDays(7);

    readonly SemaphoreSlim _gate = new(1, 1);

    public ReconciliationStatusDto Status { get; private set; } = new(null, null, 0);

    public event EventHandler? Changed;

    public async Task<Result<ReconciliationRunDto>> RunAsync(CancellationToken cancellationToken = default)
    {
        if (!await _gate.WaitAsync(0, cancellationToken))
            return Result.Failure<ReconciliationRunDto>("Đang đối soát, hãy chờ lần hiện tại xong.");
        try
        {
            var result = await RunCoreAsync(cancellationToken);
            Status = new ReconciliationStatusDto(UtcNow, result.IsSuccess ? null : result.Error,
                await reconciliations.CountUnreviewedProblemsAsync(CancellationToken.None));
            return result;
        }
        finally
        {
            _gate.Release();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<ReconciliationDayDto> GetDayAsync(DateOnly vietnamDate, CancellationToken cancellationToken = default)
    {
        var from = VietnamTime.ToUtc(vietnamDate.ToDateTime(TimeOnly.MinValue));
        var rows = await reconciliations.GetPurchasedBetweenAsync(from, from.AddDays(1), cancellationToken);
        var hasAny = rows.Count > 0 || await reconciliations.AnyAsync(cancellationToken);

        int Count(ReconciliationOutcomeEnum outcome) => rows.Count(r => r.Outcome == outcome);
        return new ReconciliationDayDto(
            [.. rows.OrderByDescending(r => r.PurchasedAtUtc).ThenByDescending(r => r.InvoiceId).Select(ToDto)],
            Count(ReconciliationOutcomeEnum.Matched),
            Count(ReconciliationOutcomeEnum.MissingDiscount),
            Count(ReconciliationOutcomeEnum.WrongAmount),
            Count(ReconciliationOutcomeEnum.UnexpectedDiscount),
            Count(ReconciliationOutcomeEnum.NearBoundary),
            rows.Where(r => r.NeedsReview && r.ExpectedDiscount > r.ActualDiscount).Sum(r => r.ExpectedDiscount - r.ActualDiscount),
            rows.Where(r => r.NeedsReview && r.ActualDiscount > r.ExpectedDiscount).Sum(r => r.ActualDiscount - r.ExpectedDiscount),
            hasAny);
    }

    public async Task MarkReviewedAsync(IReadOnlyCollection<long> invoiceIds, CancellationToken cancellationToken = default)
    {
        if (invoiceIds.Count == 0) return;
        await reconciliations.MarkReviewedAsync(invoiceIds, cancellationToken);
        Status = Status with { UnreviewedProblems = await reconciliations.CountUnreviewedProblemsAsync(cancellationToken) };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    async Task<Result<ReconciliationRunDto>> RunCoreAsync(CancellationToken cancellationToken)
    {
        var connection = await connections.GetAsync(cancellationToken);
        if (connection is null) return Result.Failure<ReconciliationRunDto>("Chưa kết nối KiotViet.");
        var secret = secretProtector.Unprotect(connection.EncryptedClientSecret);
        if (string.IsNullOrEmpty(secret))
            return Result.Failure<ReconciliationRunDto>("Không đọc được Client Secret đã lưu. Hãy nhập lại trong Kết nối KiotViet.");

        // Nothing was ever sent to the cashiers: there is nothing to check yet.
        if (await snapshots.GetFirstPublishedAtAsync(cancellationToken) is not { } firstPublishedAt)
            return Result.Success(new ReconciliationRunDto(0, 0));

        var now = UtcNow;
        var to = now - SettleDelay;
        var from = connection.InvoicesReconciledToUtc is { } done ? done - Overlap : now - FirstRunLookback;
        if (from < firstPublishedAt) from = firstPublishedAt;
        if (from >= to) return Result.Success(new ReconciliationRunDto(0, 0));

        List<KiotVietInvoiceDto> invoices;
        try
        {
            invoices = await FetchInvoicesAsync(new KiotVietCredentialsDto(connection.Retailer, connection.ClientId, secret), from, to,
                cancellationToken);
        }
        catch (KiotVietApiException ex)
        {
            logger.LogWarning(ex, "Invoice reconciliation could not read invoices");
            return Result.Failure<ReconciliationRunDto>(ex.Message);
        }

        var feeds = await snapshots.GetFromAsync(from - InvoiceReconciliation.FeedPropagation, cancellationToken);
        var existing = await reconciliations.GetByIdsAsync([.. invoices.Select(i => i.Id)], cancellationToken);
        var added = new List<InvoiceReconciliation>();
        var updated = new List<InvoiceReconciliation>();
        var newProblems = new List<InvoiceReconciliation>();

        foreach (var invoice in invoices.Where(i => i.IsCompleted && i.Lines.Count > 0))
        {
            var held = FeedsHeldAt(feeds, invoice.PurchasedAtUtc);
            if (held.Count == 0) continue;
            var check = InvoiceReconciliation.Create(invoice.Id, invoice.Code, invoice.PurchasedAtUtc, invoice.BranchName,
                invoice.SoldByName, invoice.Total, invoice.Discount, invoice.Lines, held, now);

            if (existing.TryGetValue(invoice.Id, out var previous))
            {
                var outcomeChanged = previous.Outcome != check.Outcome;
                previous.ReplaceWith(check);
                updated.Add(previous);
                if (outcomeChanged && previous.NeedsReview) newProblems.Add(previous);
            }
            else
            {
                added.Add(check);
                if (check.NeedsReview) newProblems.Add(check);
            }
        }

        await reconciliations.SaveAsync(added, updated, CancellationToken.None);
        await connections.UpdateInvoicesReconciledToAsync(to, CancellationToken.None);
        logger.LogInformation("Reconciled {Invoices} invoices from {From:o} to {To:o}: {Problems} new problems",
            added.Count + updated.Count, from, to, newProblems.Count);

        if (newProblems.Count > 0) await telegram.NotifyAsync(ProblemMessage(newProblems), CancellationToken.None);
        return Result.Success(new ReconciliationRunDto(added.Count + updated.Count, newProblems.Count));
    }

    async Task<List<KiotVietInvoiceDto>> FetchInvoicesAsync(KiotVietCredentialsDto credentials, DateTime from, DateTime to,
        CancellationToken cancellationToken)
    {
        var byId = new Dictionary<long, KiotVietInvoiceDto>();
        var read = 0;
        while (true)
        {
            var page = await api.GetInvoicesPageAsync(credentials, from, to, read, cancellationToken);
            read += page.Items.Count;
            foreach (var invoice in page.Items) byId[invoice.Id] = invoice; // offset paging can repeat items
            if (page.Items.Count == 0 || read >= page.Total) return [.. byId.Values];
        }
    }

    // The feed in effect at the purchase plus any it replaced too recently to have reached every cashier.
    static List<DiscountFeedSnapshot> FeedsHeldAt(IReadOnlyList<DiscountFeedSnapshot> feeds, DateTime purchasedAtUtc)
    {
        var last = -1;
        for (var i = 0; i < feeds.Count && feeds[i].PublishedAtUtc <= purchasedAtUtc; i++) last = i;
        if (last < 0) return [];
        var first = last;
        while (first > 0 && feeds[first].PublishedAtUtc > purchasedAtUtc - InvoiceReconciliation.FeedPropagation) first--;
        return [.. feeds.Skip(first).Take(last - first + 1)];
    }

    static string ProblemMessage(IReadOnlyList<InvoiceReconciliation> problems)
    {
        int Count(ReconciliationOutcomeEnum outcome) => problems.Count(p => p.Outcome == outcome);
        var parts = new[]
        {
            (Count(ReconciliationOutcomeEnum.MissingDiscount), "thiếu giảm giá"),
            (Count(ReconciliationOutcomeEnum.WrongAmount), "giảm sai số tiền"),
            (Count(ReconciliationOutcomeEnum.UnexpectedDiscount), "giảm ngoài chương trình"),
        }.Where(p => p.Item1 > 0).Select(p => $"{p.Item1} {p.Item2}");
        return $"KiotViet Tool – đối soát giảm giá: {problems.Count} hoá đơn cần xem ({string.Join(", ", parts)}). "
            + "Mở tool → Đối soát giảm giá để xem chi tiết.";
    }

    static InvoiceReconciliationDto ToDto(InvoiceReconciliation r) =>
        new(r.InvoiceId, r.Code, r.PurchasedAtUtc, r.BranchName, r.SoldByName, r.Total, r.ActualDiscount, r.ExpectedDiscount,
            r.Outcome, r.ProgramNames, r.NeedsReview, r.IsReviewed, r.Lines);

    DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
}
