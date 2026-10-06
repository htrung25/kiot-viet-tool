using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;

namespace KiotVietTool.Application.Interfaces;

public interface IInvoiceReconciliationService
{
    ReconciliationStatusDto Status { get; }
    event EventHandler? Changed;
    Task<Result<ReconciliationRunDto>> RunAsync(CancellationToken cancellationToken = default);
    Task<ReconciliationDayDto> GetDayAsync(DateOnly vietnamDate, CancellationToken cancellationToken = default);
    Task MarkReviewedAsync(IReadOnlyCollection<long> invoiceIds, CancellationToken cancellationToken = default);
}
