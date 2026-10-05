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
    readonly IDiscountFeedService _feed;
    readonly INavigationService _navigation;
    readonly IDialogService _dialogs;
    readonly INotificationService _notifications;
    int _programId;

    public DiscountProgramDetailViewModel(IDiscountProgramService programs, IDiscountFeedService feed,
        INavigationService navigation, IDialogService dialogs, INotificationService notifications)
    {
        _programs = programs;
        _feed = feed;
        _navigation = navigation;
        _dialogs = dialogs;
        _notifications = notifications;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name), nameof(StatusText), nameof(IsStatusGood), nameof(ValueText), nameof(StartText),
        nameof(EndText), nameof(ScopeText), nameof(Note), nameof(HasNote), nameof(CountsText), nameof(CanApply), nameof(CanStop),
        nameof(StopText), nameof(CanChangeEnd), nameof(CanEdit))]
    public partial DiscountProgramDetailDto? Detail { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRows), nameof(ShowNoRows))]
    public partial IReadOnlyList<PreviewRowItem> Rows { get; private set; } = [];

    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string? FeedWarning { get; private set; }
    [ObservableProperty] public partial DateTime? NewEndDate { get; set; }
    [ObservableProperty] public partial TimeSpan? NewEndTime { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(CanApply), nameof(CanStop), nameof(CanChangeEnd), nameof(CanEdit))]
    public partial bool IsBusy { get; private set; }

    public bool IsIdle => !IsBusy;
    public string Name => Detail?.Name ?? "";
    ProgramPhaseEnum? Phase => Detail?.Phase;
    public string StatusText => Phase is { } phase ? DisplayFormat.Phase(phase) : "";
    public bool IsStatusGood => Phase is ProgramPhaseEnum.Upcoming or ProgramPhaseEnum.Live;
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
    public string CountsText => Detail is null ? "" :
        $"{DisplayFormat.Number(Detail.Preview.AppliedCount)} sản phẩm được giảm giá"
        + (Detail.Preview.ExcludedCount > 0 ? $" · {DisplayFormat.Number(Detail.Preview.ExcludedCount)} bị loại" : "");
    public bool HasRows => Rows.Count > 0;
    public bool ShowNoRows => Detail is not null && Rows.Count == 0;

    public bool CanApply => IsIdle && Phase == ProgramPhaseEnum.Draft;
    public bool CanEdit => IsIdle && Phase == ProgramPhaseEnum.Draft;
    public bool CanStop => IsIdle && Phase is ProgramPhaseEnum.Upcoming or ProgramPhaseEnum.Live;
    public string StopText => Phase == ProgramPhaseEnum.Upcoming ? "Huỷ áp dụng" : "Dừng chương trình";
    public bool CanChangeEnd => IsIdle && Phase is ProgramPhaseEnum.Upcoming or ProgramPhaseEnum.Live;

    public override async Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        var request = parameter as ProgramDetailRequest ?? (parameter is int id ? new ProgramDetailRequest(id) : null);
        if (request is null) return;
        _programId = request.ProgramId;
        _feed.Changed += OnFeedChanged;
        await ReloadAsync(cancellationToken);
        if (request.ApplyNow && CanApply) await ApplyAsync(cancellationToken);
    }

    [RelayCommand]
    async Task ApplyAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        await ReloadAsync(cancellationToken);
        if (Detail is not { } detail) return;
        var when = detail.StartAtUtc is { } start ? $"từ {DisplayFormat.DateTime(start)}" : "ngay bây giờ";
        var confirmed = await _dialogs.ConfirmAsync("Áp dụng chương trình giảm giá?",
            $"Máy thu ngân sẽ tự giảm giá {DisplayFormat.Number(detail.Preview.AppliedCount)} sản phẩm lúc bấm Thanh toán, {when} " +
            $"đến {DisplayFormat.DateTime(detail.EndAtUtc)}. Giá bán trên KiotViet không đổi.",
            "Áp dụng");
        if (!confirmed) return;
        await RunAsync(token => _feed.ActivateAsync(_programId, token));
    }

    [RelayCommand]
    async Task StopAsync(CancellationToken cancellationToken)
    {
        var upcoming = Phase == ProgramPhaseEnum.Upcoming;
        var confirmed = await _dialogs.ConfirmAsync(
            upcoming ? "Huỷ áp dụng chương trình?" : "Dừng chương trình?",
            upcoming
                ? "Chương trình chưa bắt đầu. Huỷ áp dụng sẽ đưa chương trình về nháp."
                : "Máy thu ngân ngừng giảm giá trong khoảng 1–2 phút. Không mở lại được chương trình đã dừng.",
            upcoming ? "Huỷ áp dụng" : "Dừng chương trình", destructive: true);
        if (!confirmed) return;
        await RunAsync(token => _feed.StopAsync(_programId, token));
    }

    [RelayCommand]
    async Task ChangeEndAsync()
    {
        if (NewEndDate is not { } date || NewEndTime is not { } time)
        {
            ErrorMessage = "Chọn ngày và giờ kết thúc mới.";
            return;
        }
        var endUtc = VietnamTime.ToUtc(date.Date + new TimeSpan(time.Hours, time.Minutes, 0));
        await RunAsync(token => _feed.ChangeEndAsync(_programId, endUtc, token));
    }

    [RelayCommand]
    Task EditAsync(CancellationToken cancellationToken) =>
        _navigation.NavigateToAsync<DiscountProgramEditorViewModel>(_programId, cancellationToken);

    [RelayCommand]
    Task BackAsync(CancellationToken cancellationToken) =>
        _navigation.NavigateToAsync<DiscountProgramListViewModel>(null, cancellationToken);

    async Task RunAsync(Func<CancellationToken, Task<Result<string>>> action)
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await action(CancellationToken.None);
            if (result.IsSuccess) _notifications.ShowSuccess(result.Value!);
            else ErrorMessage = result.Error;
        }
        finally
        {
            IsBusy = false;
            await ReloadAsync(CancellationToken.None);
        }
    }

    async Task ReloadAsync(CancellationToken cancellationToken)
    {
        Detail = await _programs.GetDetailAsync(_programId, cancellationToken);
        Rows = Detail is null ? [] : [.. Detail.Preview.Rows.Where(r => r.Reason is null).Select(r => new PreviewRowItem(r))];
        FeedWarning = Phase is ProgramPhaseEnum.Upcoming or ProgramPhaseEnum.Live or ProgramPhaseEnum.Draft
            ? DisplayFormat.FeedWarning(_feed.Status)
            : null;
        if (Detail is not null)
        {
            var end = VietnamTime.FromUtc(Detail.EndAtUtc);
            NewEndDate = end.Date;
            NewEndTime = end.TimeOfDay;
        }
    }

    async void OnFeedChanged(object? sender, EventArgs e)
    {
        if (_navigation.CurrentViewModel != this)
        {
            _feed.Changed -= OnFeedChanged;
            return;
        }
        if (IsBusy) return;
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ReloadAsync(CancellationToken.None));
    }
}
