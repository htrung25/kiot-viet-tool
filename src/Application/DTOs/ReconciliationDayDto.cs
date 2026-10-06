namespace KiotVietTool.Application.DTOs;

// MissingAmount: discount the cashier extension should have given but did not; ExtraAmount: given beyond the programs.
public sealed record ReconciliationDayDto(IReadOnlyList<InvoiceReconciliationDto> Invoices, int Matched, int MissingDiscount,
    int WrongAmount, int UnexpectedDiscount, int NearBoundary, decimal MissingAmount, decimal ExtraAmount, bool HasAnyData);
