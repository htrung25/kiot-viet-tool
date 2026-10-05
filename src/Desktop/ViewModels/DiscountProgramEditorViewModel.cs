using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Common;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Models;
using KiotVietTool.Desktop.Services;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class DiscountProgramEditorViewModel : ViewModelBase
{
    readonly IDiscountProgramService _programs;
    readonly ICatalogService _catalog;
    readonly TimeProvider _timeProvider;
    readonly INavigationService _navigation;
    readonly IDialogService _dialogs;
    readonly INotificationService _notifications;
    int? _programId;
    IReadOnlyList<PreviewRowItem> _previewRows = [];

    public DiscountProgramEditorViewModel(IDiscountProgramService programs, ICatalogService catalog, TimeProvider timeProvider,
        INavigationService navigation, IDialogService dialogs, INotificationService notifications)
    {
        _programs = programs;
        _catalog = catalog;
        _timeProvider = timeProvider;
        _navigation = navigation;
        _dialogs = dialogs;
        _notifications = notifications;
        ScopeProducts = new ProductPickerViewModel(catalog, "Tìm theo mã hoặc tên hàng để thêm", "Chưa chọn sản phẩm nào.");
        ExcludedProducts = new ProductPickerViewModel(catalog, "Tìm sản phẩm cần loại trừ", "Không loại trừ sản phẩm nào.");
        SelectedPreviewFilter = PreviewFilterOptions[0];
        var now = NowVietnam;
        var nextHour = now.Date.AddHours(now.Hour + 1);
        StartDate = nextHour.Date;
        StartTime = nextHour.TimeOfDay;
        SetEnd(RoundUpToFiveMinutes(now.AddDays(3)));
    }

    public ProductPickerViewModel ScopeProducts { get; }
    public ProductPickerViewModel ExcludedProducts { get; }

    public IReadOnlyList<FilterOption<int>> PreviewFilterOptions { get; } =
    [
        new("Tất cả sản phẩm", 0),
        new("Được giảm giá", 1),
        new("Bị loại", 2),
        new("Có cảnh báo", 3),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1), nameof(IsStep2), nameof(IsStep3), nameof(CanGoBack), nameof(IsLastStep), nameof(IsNotLastStep))]
    public partial int Step { get; private set; } = 1;

    [ObservableProperty] public partial string Title { get; private set; } = "Tạo chương trình giảm giá";
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty] public partial string Name { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValueSuffix), nameof(ValueHint))]
    public partial bool IsPercent { get; set; } = true;

    [ObservableProperty] public partial string ValueText { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText))]
    public partial bool IsStartNow { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText))]
    public partial DateTime? StartDate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText))]
    public partial TimeSpan? StartTime { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText))]
    public partial DateTime? EndDate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText))]
    public partial TimeSpan? EndTime { get; set; }

    [ObservableProperty] public partial string? Note { get; set; }

    [ObservableProperty] public partial bool IsScopeAll { get; set; } = true;
    [ObservableProperty] public partial bool IsScopeCategories { get; set; }
    [ObservableProperty] public partial bool IsScopeProducts { get; set; }
    [ObservableProperty] public partial IReadOnlyList<CategoryChoice> Categories { get; private set; } = [];
    [ObservableProperty] public partial bool BaseUnitOnly { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreview), nameof(AppliedText), nameof(ExcludedText), nameof(TotalDiscountText),
        nameof(HasConflicts), nameof(ConflictText), nameof(HasOverlappingPriceBooks), nameof(OverlappingText),
        nameof(HighDiscountCount), nameof(HasHighDiscount), nameof(HighDiscountText), nameof(PeriodText))]
    public partial DiscountPreviewDto? Preview { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviewRows))]
    public partial IReadOnlyList<PreviewRowItem> PreviewRows { get; private set; } = [];

    [ObservableProperty] public partial FilterOption<int>? SelectedPreviewFilter { get; set; }
    [ObservableProperty] public partial string? PreviewKeyword { get; set; }

    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public bool IsStep3 => Step == 3;
    public bool CanGoBack => Step > 1;
    public bool IsLastStep => Step == 3;
    public bool IsNotLastStep => Step < 3;
    public bool IsIdle => !IsBusy;
    public string ValueSuffix => IsPercent ? "%" : "₫";
    public string ValueHint => IsPercent ? "Lớn hơn 0 và nhỏ hơn 100, tối đa 2 chữ số thập phân." : "Số tiền giảm trên mỗi sản phẩm, ví dụ 20.000.";
    public string DurationText
    {
        get
        {
            var start = IsStartNow ? NowVietnam : Combine(StartDate, StartTime);
            if (start is null || Combine(EndDate, EndTime) is not { } end || end <= start) return "";
            var duration = end - start.Value;
            var parts = new List<string>();
            if (duration.Days > 0) parts.Add($"{duration.Days} ngày");
            if (duration.Hours > 0) parts.Add($"{duration.Hours} giờ");
            if (duration.Days == 0 && duration.Minutes > 0) parts.Add($"{duration.Minutes} phút");
            return parts.Count == 0 ? "" : "Thời lượng: " + string.Join(" ", parts);
        }
    }

    public string PeriodText => BuildRequest(ScopeEnum.AllProducts, reportErrors: false) is { } r
        ? $"Thời gian: {(r.StartAtUtc is { } s ? DisplayFormat.DateTime(s) : "ngay khi áp dụng")} → {DisplayFormat.DateTime(r.EndAtUtc)}"
        : "";

    public bool HasPreview => Preview is not null;
    public bool HasPreviewRows => PreviewRows.Count > 0;
    public string AppliedText => DisplayFormat.Number(Preview?.AppliedCount ?? 0);
    public string ExcludedText => DisplayFormat.Number(Preview?.ExcludedCount ?? 0);
    public string TotalDiscountText => DisplayFormat.Money(Preview?.TotalDiscount ?? 0);
    public bool HasConflicts => Preview?.ConflictCount > 0;
    public string ConflictText => $"{Preview?.ConflictCount} sản phẩm đang thuộc chương trình khác trong cùng thời gian. Loại trừ các sản phẩm này ở bước Phạm vi hoặc chọn thời gian khác.";
    public bool HasOverlappingPriceBooks => Preview?.OverlappingPriceBooks.Count > 0;
    public string OverlappingText => Preview is { } p
        ? "Sản phẩm đang nằm trong bảng giá KiotViet khác có thời gian chồng lấn; nếu thu ngân chọn bảng giá đó, tiền giảm vẫn tính theo % trên giá của bảng giá đó: "
          + string.Join(", ", p.OverlappingPriceBooks.Select(b => $"{b.Name} ({b.ProductCount} SP)"))
        : "";
    public int HighDiscountCount => Preview?.Rows.Count(r => r.HasHighDiscount) ?? 0;
    public bool HasHighDiscount => HighDiscountCount > 0;
    public string HighDiscountText => $"{DisplayFormat.Number(HighDiscountCount)} sản phẩm được giảm từ 50% trở lên. Kiểm tra lại mức giảm.";

    public override async Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        SaveDiscountProgramDto? existing = parameter switch
        {
            int id => await _programs.GetForEditAsync(id, cancellationToken),
            SaveDiscountProgramDto dto => dto,
            _ => null,
        };
        _programId = existing?.Id;
        Title = _programId is null ? "Tạo chương trình giảm giá" : "Sửa chương trình giảm giá";

        var categories = await _catalog.GetCategoriesAsync(cancellationToken);
        var selectedCategories = existing?.CategoryIds.ToHashSet() ?? [];
        Categories = [.. CategoryTree.Flatten(categories)
            .Select(x => new CategoryChoice(x.Category.Id, x.Category.Name, x.Depth) { IsSelected = selectedCategories.Contains(x.Category.Id) })];

        if (existing is null) return;
        Name = existing.Name;
        IsPercent = existing.Type == DiscountEnum.Percent;
        ValueText = IsPercent
            ? existing.Value.ToString("0.##", DisplayFormat.Vietnamese)
            : existing.Value.ToString("#,##0", DisplayFormat.Vietnamese);
        IsStartNow = existing.StartMode == StartModeEnum.Immediately;
        if (existing.StartAtUtc is { } startAt)
        {
            var startLocal = VietnamTime.FromUtc(startAt);
            StartDate = startLocal.Date;
            StartTime = startLocal.TimeOfDay;
        }
        SetEnd(VietnamTime.FromUtc(existing.EndAtUtc));
        Note = existing.Note;
        IsScopeAll = existing.Scope == ScopeEnum.AllProducts;
        IsScopeCategories = existing.Scope == ScopeEnum.Categories;
        IsScopeProducts = existing.Scope == ScopeEnum.Products;
        BaseUnitOnly = existing.UnitScope == UnitScopeEnum.BaseUnitOnly;
        await ScopeProducts.LoadAsync([.. existing.ProductIds], cancellationToken);
        await ExcludedProducts.LoadAsync([.. existing.ExcludedProductIds], cancellationToken);
    }

    [RelayCommand]
    async Task NextAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        if (Step == 1)
        {
            if (BuildRequest(ScopeEnum.AllProducts) is not { } basics) return;
            IsBusy = true;
            var check = await _programs.PreviewAsync(basics, cancellationToken);
            IsBusy = false;
            if (!check.IsSuccess)
            {
                ErrorMessage = check.Error;
                return;
            }
            Step = 2;
            return;
        }

        if (BuildRequest() is not { } request) return;
        IsBusy = true;
        var result = await _programs.PreviewAsync(request, cancellationToken);
        IsBusy = false;
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return;
        }

        Preview = result.Value;
        _previewRows = [.. result.Value!.Rows.Select(r => new PreviewRowItem(r))];
        ApplyPreviewFilter();
        Step = 3;
    }

    [RelayCommand]
    void Back()
    {
        ErrorMessage = null;
        if (Step > 1) Step--;
    }

    [RelayCommand]
    Task CancelAsync(CancellationToken cancellationToken) =>
        _navigation.NavigateToAsync<DiscountProgramListViewModel>(null, cancellationToken);

    [RelayCommand]
    async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        if (BuildRequest() is not { } request || Preview is null) return;

        IsBusy = true;
        var result = await _programs.SaveAsync(request, cancellationToken);
        IsBusy = false;
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return;
        }

        _notifications.ShowSuccess(_programId is null ? "Đã lưu chương trình nháp." : "Đã lưu thay đổi.");
        await _navigation.NavigateToAsync<DiscountProgramListViewModel>(null, cancellationToken);
    }

    [RelayCommand]
    async Task SaveAndApplyAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        if (BuildRequest() is not { } request || Preview is null) return;

        IsBusy = true;
        var result = await _programs.SaveAsync(request, cancellationToken);
        IsBusy = false;
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return;
        }
        await _navigation.NavigateToAsync<DiscountProgramDetailViewModel>(new ProgramDetailRequest(result.Value, ApplyNow: true), cancellationToken);
    }

    [RelayCommand]
    void SetDuration(string days)
    {
        var start = IsStartNow ? NowVietnam : Combine(StartDate, StartTime) ?? NowVietnam;
        SetEnd(RoundUpToFiveMinutes(start.AddDays(int.Parse(days, CultureInfo.InvariantCulture))));
    }

    partial void OnSelectedPreviewFilterChanged(FilterOption<int>? value) => ApplyPreviewFilter();
    partial void OnPreviewKeywordChanged(string? value) => ApplyPreviewFilter();

    void ApplyPreviewFilter()
    {
        IEnumerable<PreviewRowItem> rows = _previewRows;
        rows = SelectedPreviewFilter?.Value switch
        {
            1 => rows.Where(r => !r.IsExcluded),
            2 => rows.Where(r => r.IsExcluded),
            3 => rows.Where(r => r.HasWarning),
            _ => rows,
        };
        if (!string.IsNullOrWhiteSpace(PreviewKeyword))
            rows = rows.Where(r => r.Code.Contains(PreviewKeyword.Trim(), StringComparison.OrdinalIgnoreCase)
                || r.Name.Contains(PreviewKeyword.Trim(), StringComparison.OrdinalIgnoreCase));
        PreviewRows = [.. rows];
    }

    SaveDiscountProgramDto? BuildRequest(ScopeEnum? scopeOverride = null, bool reportErrors = true)
    {
        string? error = null;
        DateTime? startAtUtc = null;
        if (!TryParseValue(out var value))
            error = IsPercent ? "Nhập mức giảm %, ví dụ 10 hoặc 12,5." : "Nhập số tiền giảm, ví dụ 20.000.";
        else if (!IsStartNow && Combine(StartDate, StartTime) is not { } start)
            error = "Chọn ngày và giờ bắt đầu.";
        else if (Combine(EndDate, EndTime) is not { } end)
            error = "Chọn ngày và giờ kết thúc.";
        else
        {
            if (!IsStartNow) startAtUtc = VietnamTime.ToUtc(Combine(StartDate, StartTime)!.Value);
            var scope = scopeOverride ?? (IsScopeCategories ? ScopeEnum.Categories : IsScopeProducts ? ScopeEnum.Products : ScopeEnum.AllProducts);
            return new SaveDiscountProgramDto(_programId, Name, IsPercent ? DiscountEnum.Percent : DiscountEnum.Amount, value,
                IsStartNow ? StartModeEnum.Immediately : StartModeEnum.Scheduled,
                startAtUtc, VietnamTime.ToUtc(end), scope,
                [.. Categories.Where(c => c.IsSelected).Select(c => c.Id)], ScopeProducts.SelectedIds, ExcludedProducts.SelectedIds,
                BaseUnitOnly ? UnitScopeEnum.BaseUnitOnly : UnitScopeEnum.AllUnits, Note);
        }

        if (reportErrors) ErrorMessage = error;
        return null;
    }

    DateTime NowVietnam => VietnamTime.FromUtc(_timeProvider.GetUtcNow().UtcDateTime);

    void SetEnd(DateTime local)
    {
        EndDate = local.Date;
        EndTime = new TimeSpan(local.Hour, local.Minute, 0);
    }

    static DateTime? Combine(DateTime? date, TimeSpan? time) =>
        date is { } d && time is { } t ? d.Date + new TimeSpan(t.Hours, t.Minutes, 0) : null;

    static DateTime RoundUpToFiveMinutes(DateTime value)
    {
        var step = TimeSpan.FromMinutes(5).Ticks;
        return new DateTime((value.Ticks + step - 1) / step * step, value.Kind);
    }

    bool TryParseValue(out decimal value)
    {
        var text = ValueText.Replace(" ", "").Replace("%", "").Replace("₫", "").Replace("đ", "");
        text = IsPercent ? text.Replace(',', '.') : text.Replace(".", "").Replace(",", "");
        return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }
}
