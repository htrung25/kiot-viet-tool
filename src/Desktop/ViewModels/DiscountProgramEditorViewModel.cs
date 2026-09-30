using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    readonly ICatalogSyncService _sync;
    readonly INavigationService _navigation;
    readonly IDialogService _dialogs;
    readonly INotificationService _notifications;
    int? _programId;
    IReadOnlyList<PreviewRowItem> _previewRows = [];

    public DiscountProgramEditorViewModel(IDiscountProgramService programs, ICatalogService catalog, ICatalogSyncService sync,
        INavigationService navigation, IDialogService dialogs, INotificationService notifications)
    {
        _programs = programs;
        _catalog = catalog;
        _sync = sync;
        _navigation = navigation;
        _dialogs = dialogs;
        _notifications = notifications;
        ScopeProducts = new ProductPickerViewModel(catalog, "Tìm theo mã hoặc tên hàng để thêm", "Chưa chọn sản phẩm nào.");
        ExcludedProducts = new ProductPickerViewModel(catalog, "Tìm sản phẩm cần loại trừ", "Không loại trừ sản phẩm nào.");
        SelectedRounding = RoundingOptions[^1];
        SelectedPreviewFilter = PreviewFilterOptions[0];
    }

    public ProductPickerViewModel ScopeProducts { get; }
    public ProductPickerViewModel ExcludedProducts { get; }

    public IReadOnlyList<FilterOption<RoundingEnum>> RoundingOptions { get; } =
    [
        new("Không làm tròn", RoundingEnum.None),
        new("Làm tròn xuống 100 ₫", RoundingEnum.Down100),
        new("Làm tròn xuống 500 ₫", RoundingEnum.Down500),
        new("Làm tròn xuống 1.000 ₫", RoundingEnum.Down1000),
    ];

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
    [ObservableProperty] public partial FilterOption<RoundingEnum>? SelectedRounding { get; set; }
    [ObservableProperty] public partial IReadOnlyList<TargetPriceBookDto> TargetPriceBooks { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTarget), nameof(TargetPeriodText), nameof(TargetScopeText))]
    public partial TargetPriceBookDto? SelectedTarget { get; set; }

    [ObservableProperty] public partial string? Note { get; set; }

    [ObservableProperty] public partial bool IsScopeAll { get; set; } = true;
    [ObservableProperty] public partial bool IsScopeCategories { get; set; }
    [ObservableProperty] public partial bool IsScopeProducts { get; set; }
    [ObservableProperty] public partial IReadOnlyList<CategoryChoice> Categories { get; private set; } = [];
    [ObservableProperty] public partial bool BaseUnitOnly { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreview), nameof(AppliedText), nameof(ExcludedText), nameof(TotalDiscountText),
        nameof(HasConflicts), nameof(ConflictText), nameof(HasOverlappingPriceBooks), nameof(OverlappingText),
        nameof(HasForeignItems), nameof(ForeignItemsText), nameof(HighDiscountCount), nameof(HasHighDiscount), nameof(HighDiscountText))]
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
    public bool HasTarget => SelectedTarget is not null;
    public string TargetPeriodText => SelectedTarget is { } t
        ? $"{DisplayFormat.DateTime(t.StartAtUtc)} – {DisplayFormat.DateTime(t.EndAtUtc)}"
        : "";
    public string TargetScopeText => SelectedTarget is { } t
        ? (t.ForAllBranches ? "Tất cả chi nhánh" : $"{t.BranchCount} chi nhánh")
          + (t.ForAllCustomerGroups ? "" : " · chỉ một số nhóm khách hàng")
          + (t.ItemCount > 0 ? $" · đang có {DisplayFormat.Number(t.ItemCount)} sản phẩm" : " · chưa có sản phẩm")
        : "";

    public bool HasPreview => Preview is not null;
    public bool HasPreviewRows => PreviewRows.Count > 0;
    public string AppliedText => DisplayFormat.Number(Preview?.AppliedCount ?? 0);
    public string ExcludedText => DisplayFormat.Number(Preview?.ExcludedCount ?? 0);
    public string TotalDiscountText => DisplayFormat.Money(Preview?.TotalDiscount ?? 0);
    public bool HasConflicts => Preview?.ConflictCount > 0;
    public string ConflictText => $"{Preview?.ConflictCount} sản phẩm đang thuộc chương trình khác trong cùng thời gian. Loại trừ các sản phẩm này ở bước Phạm vi hoặc chọn bảng giá có thời gian khác.";
    public bool HasOverlappingPriceBooks => Preview?.OverlappingPriceBooks.Count > 0;
    public string OverlappingText => Preview is { } p
        ? "Sản phẩm cũng nằm trong bảng giá khác có thời gian chồng lấn, KiotViet có thể áp bảng giá đó: "
          + string.Join(", ", p.OverlappingPriceBooks.Select(b => $"{b.Name} ({b.ProductCount} SP)"))
        : "";
    public bool HasForeignItems => Preview?.ForeignItemsInTarget > 0;
    public string ForeignItemsText => $"Bảng giá đích đang có {Preview?.ForeignItemsInTarget} sản phẩm không thuộc chương trình. Giá của các sản phẩm này trên KiotViet không bị tool thay đổi.";
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

        TargetPriceBooks = await _programs.GetTargetPriceBooksAsync(_programId, cancellationToken);
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
        SelectedRounding = RoundingOptions.FirstOrDefault(o => o.Value == existing.Rounding) ?? RoundingOptions[^1];
        SelectedTarget = TargetPriceBooks.FirstOrDefault(b => b.Id == existing.TargetPriceBookId);
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
        if (BuildRequest() is not { } request || Preview is not { } preview) return;

        var acknowledge = false;
        if (preview.ForeignItemsInTarget > 0)
        {
            acknowledge = await _dialogs.ConfirmAsync("Dùng bảng giá đang có sản phẩm khác?",
                $"Bảng giá \"{preview.Target.Name}\" đang có {preview.ForeignItemsInTarget} sản phẩm không thuộc chương trình. " +
                "Các sản phẩm đó vẫn giữ giá hiện tại trong bảng giá khi chương trình chạy.",
                "Vẫn dùng bảng giá này");
            if (!acknowledge) return;
        }

        IsBusy = true;
        var result = await _programs.SaveAsync(request, acknowledge, cancellationToken);
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
    async Task SyncPriceBooksAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        IsBusy = true;
        var result = await _sync.SyncAsync(null, cancellationToken);
        IsBusy = false;
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error;
            return;
        }

        var selectedId = SelectedTarget?.Id;
        TargetPriceBooks = await _programs.GetTargetPriceBooksAsync(_programId, cancellationToken);
        SelectedTarget = TargetPriceBooks.FirstOrDefault(b => b.Id == selectedId);
        _notifications.ShowSuccess($"Đã đồng bộ, có {DisplayFormat.Number(TargetPriceBooks.Count)} bảng giá.");
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

    SaveDiscountProgramDto? BuildRequest(ScopeEnum? scopeOverride = null)
    {
        if (!TryParseValue(out var value))
        {
            ErrorMessage = IsPercent ? "Nhập mức giảm %, ví dụ 10 hoặc 12,5." : "Nhập số tiền giảm, ví dụ 20.000.";
            return null;
        }

        var scope = scopeOverride ?? (IsScopeCategories ? ScopeEnum.Categories : IsScopeProducts ? ScopeEnum.Products : ScopeEnum.AllProducts);
        return new SaveDiscountProgramDto(_programId, Name, IsPercent ? DiscountEnum.Percent : DiscountEnum.Amount, value,
            SelectedRounding?.Value ?? RoundingEnum.Down1000, SelectedTarget?.Id, scope,
            [.. Categories.Where(c => c.IsSelected).Select(c => c.Id)], ScopeProducts.SelectedIds, ExcludedProducts.SelectedIds,
            BaseUnitOnly ? UnitScopeEnum.BaseUnitOnly : UnitScopeEnum.AllUnits, Note);
    }

    bool TryParseValue(out decimal value)
    {
        var text = ValueText.Replace(" ", "").Replace("%", "").Replace("₫", "").Replace("đ", "");
        text = IsPercent ? text.Replace(',', '.') : text.Replace(".", "").Replace(",", "");
        return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }
}
