using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Models;
using KiotVietTool.Desktop.Services;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class InvoiceReconciliationViewModel(
    IInvoiceReconciliationService reconciliation,
    IDialogService dialogs,
    INotificationService notifications,
    TimeProvider timeProvider) : ViewModelBase
{
    const int AllFilter = 0;
    const int NeedsReviewFilter = -1;

    IReadOnlyList<ReconciliationRowItem> _all = [];

    public IReadOnlyList<FilterOption<int>> FilterOptions { get; } =
    [
        new("Tất cả hoá đơn", AllFilter),
        new("Cần xem", NeedsReviewFilter),
        .. Enum.GetValues<ReconciliationOutcomeEnum>().Select(o => new FilterOption<int>(DisplayFormat.Outcome(o), (int)o)),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRows), nameof(ShowNoResults))]
    public partial IReadOnlyList<ReconciliationRowItem> Rows { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial ReconciliationRowItem? SelectedRow { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState), nameof(ShowNoResults), nameof(SummaryText), nameof(AmountText), nameof(HasAmount))]
    public partial ReconciliationDayDto? Day { get; private set; }

    [ObservableProperty] public partial DateTime? SelectedDate { get; set; }
    [ObservableProperty] public partial FilterOption<int>? SelectedFilter { get; set; }
    [ObservableProperty] public partial string? StatusText { get; private set; }
    [ObservableProperty] public partial string? ErrorMessage { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }

    public bool IsIdle => !IsBusy;
    public bool HasRows => Rows.Count > 0;
    public bool HasSelection => SelectedRow is not null;
    public bool ShowEmptyState => Day is { HasAnyData: false };
    public bool ShowNoResults => Day is { HasAnyData: true } && Rows.Count == 0;
    public bool HasAmount => Day is { } d && d.MissingAmount + d.ExtraAmount > 0;

    public string SummaryText => Day is not { } d ? "" :
        $"{DisplayFormat.Number(d.Invoices.Count)} hoá đơn · {DisplayFormat.Number(d.Matched)} khớp · "
        + $"{DisplayFormat.Number(d.MissingDiscount)} thiếu giảm · {DisplayFormat.Number(d.WrongAmount)} giảm sai · "
        + $"{DisplayFormat.Number(d.UnexpectedDiscount)} ngoài chương trình · {DisplayFormat.Number(d.NearBoundary)} sát giờ";

    public string AmountText => Day is not { } d ? "" :
        $"Thiếu giảm {DisplayFormat.Money(d.MissingAmount)} (khách trả thừa) · Giảm thừa {DisplayFormat.Money(d.ExtraAmount)} (cửa hàng thiệt)";

    public override async Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        SelectedFilter = FilterOptions[0];
        SelectedDate = VietnamTime.FromUtc(timeProvider.GetUtcNow().UtcDateTime).Date;
        await ReloadAsync(cancellationToken);
    }

    async partial void OnSelectedDateChanged(DateTime? value)
    {
        if (Day is not null) await ReloadAsync(CancellationToken.None);
    }

    partial void OnSelectedFilterChanged(FilterOption<int>? value) => ApplyFilter();

    [RelayCommand]
    async Task RunAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await Task.Run(() => reconciliation.RunAsync(cancellationToken), cancellationToken);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error;
                return;
            }
            notifications.ShowSuccess(result.Value!.NewProblems > 0
                ? $"Đã đối soát {DisplayFormat.Number(result.Value.CheckedInvoices)} hoá đơn, {DisplayFormat.Number(result.Value.NewProblems)} hoá đơn cần xem."
                : $"Đã đối soát {DisplayFormat.Number(result.Value.CheckedInvoices)} hoá đơn.");
        }
        finally
        {
            IsBusy = false;
        }
        await ReloadAsync(cancellationToken);
    }

    [RelayCommand]
    async Task MarkReviewedAsync(CancellationToken cancellationToken)
    {
        var ids = Rows.Where(r => r.NeedsReview).Select(r => r.InvoiceId).ToList();
        if (ids.Count == 0) return;
        var confirmed = await dialogs.ConfirmAsync("Đánh dấu đã xem?",
            $"{DisplayFormat.Number(ids.Count)} hoá đơn đang hiển thị sẽ không còn tính trong cảnh báo. Có thể xem lại bất cứ lúc nào.",
            "Đánh dấu đã xem");
        if (!confirmed) return;
        await reconciliation.MarkReviewedAsync(ids, cancellationToken);
        await ReloadAsync(cancellationToken);
    }

    [RelayCommand]
    void PreviousDay() => SelectedDate = (SelectedDate ?? DateTime.Today).AddDays(-1);

    [RelayCommand]
    void NextDay() => SelectedDate = (SelectedDate ?? DateTime.Today).AddDays(1);

    async Task ReloadAsync(CancellationToken cancellationToken)
    {
        var date = DateOnly.FromDateTime(SelectedDate ?? DateTime.Today);
        var selectedId = SelectedRow?.InvoiceId;
        Day = await reconciliation.GetDayAsync(date, cancellationToken);
        _all = [.. Day.Invoices.Select(i => new ReconciliationRowItem(i))];
        ApplyFilter();
        SelectedRow = Rows.FirstOrDefault(r => r.InvoiceId == selectedId);

        var status = reconciliation.Status;
        StatusText = status.LastRunAtUtc is { } at
            ? $"Đối soát gần nhất: {DisplayFormat.DateTime(at)}" + (status.LastError is { } error ? $" (lỗi: {error})" : "")
            : "Tool tự đối soát mỗi 15 phút khi đang mở.";
    }

    void ApplyFilter() => Rows = SelectedFilter?.Value switch
    {
        null or AllFilter => _all,
        NeedsReviewFilter => [.. _all.Where(r => r.NeedsReview)],
        var outcome => [.. _all.Where(r => (int)r.Invoice.Outcome == outcome)],
    };
}
