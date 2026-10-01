using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Models;
using KiotVietTool.Desktop.Services;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class DiscountProgramDetailViewModel : ViewModelBase
{
    readonly IDiscountProgramService _programs;
    readonly IPriceDeploymentService _deployment;
    readonly INavigationService _navigation;
    readonly IDialogService _dialogs;
    readonly INotificationService _notifications;
    readonly TimeProvider _timeProvider;
    int _programId;
    CancellationTokenSource? _operation;

    public DiscountProgramDetailViewModel(IDiscountProgramService programs, IPriceDeploymentService deployment,
        INavigationService navigation, IDialogService dialogs, INotificationService notifications, TimeProvider timeProvider)
    {
        _programs = programs;
        _deployment = deployment;
        _navigation = navigation;
        _dialogs = dialogs;
        _notifications = notifications;
        _timeProvider = timeProvider;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name), nameof(StatusText), nameof(IsStatusGood), nameof(IsStatusBad), nameof(ValueText),
        nameof(StartText), nameof(EndText), nameof(ScopeText), nameof(Note), nameof(HasNote), nameof(CountsText), nameof(CanApply),
        nameof(CanStop), nameof(StopText), nameof(CanRetry), nameof(CanChangeEnd), nameof(CanEdit), nameof(HasManualChanges),
        nameof(ManualChangesText), nameof(HasPrices), nameof(ShowNotAppliedHint), nameof(IsOverdue))]
    public partial DiscountProgramDetailDto? Detail { get; private set; }

    [ObservableProperty] public partial IReadOnlyList<ProgramPriceItem> Prices { get; private set; } = [];
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string? ProgressText { get; private set; }
    [ObservableProperty] public partial double ProgressValue { get; private set; }
    [ObservableProperty] public partial DateTime? NewEndDate { get; set; }
    [ObservableProperty] public partial TimeSpan? NewEndTime { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(CanApply), nameof(CanStop), nameof(CanRetry), nameof(CanChangeEnd), nameof(CanEdit))]
    public partial bool IsBusy { get; private set; }

    public bool IsIdle => !IsBusy;
    public string Name => Detail?.Name ?? "";
    public ProgramStatusEnum? Status => Detail?.Status;
    public bool IsOverdue => Detail?.IsOverdue == true;
    public string StatusText => Detail is null ? "" : IsOverdue ? "Quá hạn — chưa trả giá" : DisplayFormat.Status(Detail.Status);
    public bool IsStatusGood => Status is ProgramStatusEnum.Scheduled or ProgramStatusEnum.Running or ProgramStatusEnum.Ended && !IsOverdue;
    public bool IsStatusBad => IsOverdue || Status is ProgramStatusEnum.ApplyFailed or ProgramStatusEnum.RestoreFailed;
    public string ValueText => Detail is null ? "" : DisplayFormat.DiscountValue(Detail.Type, Detail.Value);
    public string StartText => Detail?.StartAtUtc is { } start ? DisplayFormat.DateTime(start) : "Ngay khi áp dụng";
    public string EndText => Detail is null ? "" : DisplayFormat.DateTime(Detail.EndAtUtc);
    public string ScopeText => Detail?.Scope switch
    {
        ScopeEnum.Categories => $"{Detail.ScopeItemCount} nhóm hàng",
        ScopeEnum.Products => $"{Detail.ScopeItemCount} sản phẩm",
        _ => "Toàn bộ sản phẩm",
    };
    public string? Note => Detail?.Note;
    public bool HasNote => !string.IsNullOrEmpty(Note);
    public string CountsText => Detail is null || Detail.Prices.Count == 0 ? "" :
        $"{Detail.Prices.Count} sản phẩm · đang giảm {Detail.AppliedCount} · đã trả giá {Detail.RestoredCount} · lỗi {Detail.FailedCount}"
        + (Detail.ChangedManuallyCount > 0 ? $" · bị sửa tay {Detail.ChangedManuallyCount}" : "");
    public bool HasPrices => Prices.Count > 0;
    public bool ShowNotAppliedHint => Detail is { Prices.Count: 0 };

    public bool CanApply => IsIdle && Status == ProgramStatusEnum.Draft;
    public bool CanEdit => IsIdle && Status is ProgramStatusEnum.Draft or ProgramStatusEnum.Scheduled;
    public bool CanStop => IsIdle && Status is ProgramStatusEnum.Scheduled or ProgramStatusEnum.Applying or ProgramStatusEnum.Running
        or ProgramStatusEnum.ApplyFailed or ProgramStatusEnum.Restoring or ProgramStatusEnum.RestoreFailed;
    public string StopText => Status == ProgramStatusEnum.Scheduled ? "Huỷ lịch" : "Dừng chương trình";
    public bool CanRetry => IsIdle && Status is ProgramStatusEnum.ApplyFailed or ProgramStatusEnum.RestoreFailed
        or ProgramStatusEnum.Applying or ProgramStatusEnum.Restoring;
    public bool CanChangeEnd => IsIdle && Status is ProgramStatusEnum.Scheduled or ProgramStatusEnum.Running or ProgramStatusEnum.ApplyFailed;
    public bool HasManualChanges => Detail?.ChangedManuallyCount > 0;
    public string ManualChangesText => $"{Detail?.ChangedManuallyCount} sản phẩm đã bị sửa giá trên KiotViet trong lúc chạy chương trình nên tool không ghi đè. Chọn giữ giá hiện tại trên KiotViet hoặc trả về giá gốc đã lưu.";

    partial void OnPricesChanged(IReadOnlyList<ProgramPriceItem> value) => OnPropertyChanged(nameof(HasPrices));

    public override async Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        var request = parameter as ProgramDetailRequest ?? (parameter is int id ? new ProgramDetailRequest(id) : null);
        if (request is null) return;
        _programId = request.ProgramId;
        _deployment.Changed += OnDeploymentChanged;
        await ReloadAsync(cancellationToken);
        if (request.ApplyNow && CanApply) await ApplyAsync(cancellationToken);
    }

    [RelayCommand]
    async Task ApplyAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        var preview = await _programs.PreviewSavedAsync(_programId, cancellationToken);
        if (!preview.IsSuccess)
        {
            ErrorMessage = preview.Error;
            return;
        }
        var p = preview.Value!;
        var detail = Detail!;
        var when = detail.StartAtUtc is { } start && start > _timeProvider.GetUtcNow().UtcDateTime
            ? $"lúc {DisplayFormat.DateTime(start)}"
            : "ngay bây giờ";
        var confirmed = await _dialogs.ConfirmAsync("Áp dụng chương trình giảm giá?",
            $"Giá bán của {p.AppliedCount} sản phẩm trên KiotViet sẽ đổi thành giá đã giảm {when}" +
            (p.ExcludedCount > 0 ? $" ({p.ExcludedCount} sản phẩm bị loại giữ nguyên giá)" : "") +
            $". Tool tự trả giá gốc lúc {DisplayFormat.DateTime(detail.EndAtUtc)}. Thu ngân bán hàng như bình thường.",
            "Áp dụng");
        if (!confirmed) return;
        await RunAsync((progress, token) => _deployment.ActivateAsync(_programId, progress, token));
    }

    [RelayCommand]
    async Task StopAsync(CancellationToken cancellationToken)
    {
        var scheduled = Status == ProgramStatusEnum.Scheduled;
        var confirmed = await _dialogs.ConfirmAsync(
            scheduled ? "Huỷ lịch chương trình?" : "Dừng chương trình?",
            scheduled
                ? "Chương trình chưa đổi giá nào trên KiotViet. Huỷ lịch sẽ đưa chương trình về nháp."
                : "Giá bán trên KiotViet sẽ trở về giá gốc ngay. Khách mua sau đó trả giá gốc.",
            scheduled ? "Huỷ lịch" : "Dừng chương trình", destructive: true);
        if (!confirmed) return;
        await RunAsync((progress, token) => _deployment.StopAsync(_programId, progress, token));
    }

    [RelayCommand]
    Task RetryAsync() =>
        RunAsync((progress, token) => _deployment.RetryAsync(_programId, progress, token));

    [RelayCommand]
    Task KeepManualChangesAsync() =>
        RunAsync((_, token) => _deployment.ResolveManualChangesAsync(_programId, restoreOriginal: false, token));

    [RelayCommand]
    Task RestoreManualChangesAsync() =>
        RunAsync((_, token) => _deployment.ResolveManualChangesAsync(_programId, restoreOriginal: true, token));

    [RelayCommand]
    async Task ChangeEndAsync()
    {
        if (NewEndDate is not { } date || NewEndTime is not { } time)
        {
            ErrorMessage = "Chọn ngày và giờ kết thúc mới.";
            return;
        }
        var endUtc = VietnamTime.ToUtc(date.Date + new TimeSpan(time.Hours, time.Minutes, 0));
        await RunAsync((_, token) => _deployment.ChangeEndAsync(_programId, endUtc, token));
    }

    [RelayCommand]
    Task EditAsync(CancellationToken cancellationToken) =>
        _navigation.NavigateToAsync<DiscountProgramEditorViewModel>(_programId, cancellationToken);

    [RelayCommand]
    Task BackAsync(CancellationToken cancellationToken) =>
        _navigation.NavigateToAsync<DiscountProgramListViewModel>(null, cancellationToken);

    [RelayCommand]
    void CancelOperation() => _operation?.Cancel();

    async Task RunAsync(Func<IProgress<DeploymentProgressDto>, CancellationToken, Task<Result<string>>> action)
    {
        using var operation = _operation = new CancellationTokenSource();
        ErrorMessage = null;
        IsBusy = true;
        ProgressText = "Đang xử lý…";
        ProgressValue = 0;
        var progress = new Progress<DeploymentProgressDto>(p =>
        {
            ProgressText = p.Total > 0 ? $"{p.Stage}: {p.Done}/{p.Total} sản phẩm" : p.Stage;
            ProgressValue = p.Total > 0 ? 100.0 * p.Done / p.Total : 0;
        });
        try
        {
            var result = await action(progress, operation.Token);
            if (result.IsSuccess) _notifications.ShowSuccess(result.Value!);
            else ErrorMessage = result.Error;
        }
        finally
        {
            _operation = null;
            IsBusy = false;
            ProgressText = null;
            await ReloadAsync(CancellationToken.None);
        }
    }

    async Task ReloadAsync(CancellationToken cancellationToken)
    {
        Detail = await _programs.GetDetailAsync(_programId, cancellationToken);
        Prices = Detail is null ? [] : [.. Detail.Prices.Select(p => new ProgramPriceItem(p))];
        if (Detail is not null)
        {
            var end = VietnamTime.FromUtc(Detail.EndAtUtc);
            NewEndDate = end.Date;
            NewEndTime = end.TimeOfDay;
        }
    }

    async void OnDeploymentChanged(object? sender, EventArgs e)
    {
        if (_navigation.CurrentViewModel != this)
        {
            _deployment.Changed -= OnDeploymentChanged;
            return;
        }
        if (IsBusy) return;
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ReloadAsync(CancellationToken.None));
    }
}
