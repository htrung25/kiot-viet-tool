using System.Globalization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Desktop.Models;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class ProductListViewModel : ViewModelBase
{
    static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(250);
    static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    static readonly FilterOption<string?> AllCategories = new("Tất cả nhóm hàng", null);

    IReadOnlyList<ProductItem> _all = [];
    IReadOnlyList<ProductItem> _filtered = [];
    bool _filterSuspended;
    CancellationTokenSource? _pendingSearch;

    public IReadOnlyList<FilterOption<int>> PageSizeOptions { get; } =
    [
        new("20 / trang", 20),
        new("50 / trang", 50),
        new("100 / trang", 100),
    ];

    public IReadOnlyList<FilterOption<ProductKind?>> KindOptions { get; } =
    [
        new("Tất cả loại hàng", null),
        new("Hàng hoá", ProductKind.Goods),
        new("Combo", ProductKind.Combo),
        new("Dịch vụ", ProductKind.Service),
    ];

    public IReadOnlyList<FilterOption<bool?>> StatusOptions { get; } =
    [
        new("Tất cả trạng thái", null),
        new("Đang kinh doanh", true),
        new("Ngừng kinh doanh", false),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProducts), nameof(ShowNoResults), nameof(Summary))]
    public partial IReadOnlyList<ProductItem> Products { get; private set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<FilterOption<string?>> CategoryOptions { get; private set; } = [AllCategories];

    [ObservableProperty] public partial string? Keyword { get; set; }
    [ObservableProperty] public partial FilterOption<string?>? SelectedCategory { get; set; } = AllCategories;
    [ObservableProperty] public partial FilterOption<ProductKind?>? SelectedKind { get; set; }
    [ObservableProperty] public partial FilterOption<bool?>? SelectedStatus { get; set; }
    [ObservableProperty] public partial FilterOption<int>? SelectedPageSize { get; set; }

    [ObservableProperty] public partial int CurrentPage { get; private set; } = 1;
    [ObservableProperty] public partial int PageCount { get; private set; } = 1;
    [ObservableProperty] public partial IReadOnlyList<PageLink> PageLinks { get; private set; } = PageLink.Build(1, 1);

    public ProductListViewModel()
    {
        SelectedKind = KindOptions[0];
        SelectedStatus = StatusOptions[0];
        SelectedPageSize = PageSizeOptions[0];
    }

    int PageSize => SelectedPageSize?.Value ?? PageSizeOptions[0].Value;

    public bool HasData => _all.Count > 0;
    public bool HasProducts => Products.Count > 0;
    public bool ShowEmptyState => !HasData;
    public bool ShowNoResults => HasData && !HasProducts;

    public string Summary
    {
        get
        {
            if (_all.Count == 0) return "0 sản phẩm";
            if (_filtered.Count == 0) return string.Format(Vietnamese, "0 / {0:N0} sản phẩm", _all.Count);
            var from = (CurrentPage - 1) * PageSize + 1;
            var to = from + Products.Count - 1;
            var range = string.Format(Vietnamese, "{0:N0}–{1:N0} / {2:N0} sản phẩm", from, to, _filtered.Count);
            return _filtered.Count == _all.Count
                ? range
                : string.Format(Vietnamese, "{0} (lọc từ {1:N0})", range, _all.Count);
        }
    }

    public void Load(IReadOnlyList<ProductItem> products)
    {
        _all = products;
        RunWithoutFiltering(() =>
        {
            CategoryOptions = [AllCategories, .. products.Select(p => p.CategoryName).Distinct().Order()
                .Select(name => new FilterOption<string?>(name, name))];
            SelectedCategory = AllCategories;
        });
        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(ShowEmptyState));
        ApplyFilter();
    }

    [RelayCommand]
    void ClearFilters()
    {
        RunWithoutFiltering(() =>
        {
            Keyword = "";
            SelectedCategory = AllCategories;
            SelectedKind = KindOptions[0];
            SelectedStatus = StatusOptions[0];
        });
        ApplyFilter();
    }

    async partial void OnKeywordChanged(string? value)
    {
        _pendingSearch?.Cancel();
        if (_filterSuspended) return;
        _pendingSearch = new CancellationTokenSource();
        try
        {
            await Task.Delay(SearchDelay, _pendingSearch.Token);
            ApplyFilter();
        }
        catch (OperationCanceledException) { }
    }

    partial void OnSelectedCategoryChanged(FilterOption<string?>? value) => ApplyFilter();
    partial void OnSelectedKindChanged(FilterOption<ProductKind?>? value) => ApplyFilter();
    partial void OnSelectedStatusChanged(FilterOption<bool?>? value) => ApplyFilter();

    partial void OnSelectedPageSizeChanged(FilterOption<int>? value)
    {
        if (!_filterSuspended) ShowPage(1);
    }

    [RelayCommand]
    void GoToPage(int? page)
    {
        if (page is { } number) ShowPage(number);
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    void PreviousPage() => ShowPage(CurrentPage - 1);

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    void NextPage() => ShowPage(CurrentPage + 1);

    bool CanGoToPreviousPage() => CurrentPage > 1;
    bool CanGoToNextPage() => CurrentPage < PageCount;

    void RunWithoutFiltering(Action changes)
    {
        _filterSuspended = true;
        try { changes(); }
        finally { _filterSuspended = false; }
    }

    void ApplyFilter()
    {
        if (_filterSuspended) return;
        var filter = new ProductFilter(Keyword, SelectedCategory?.Value, SelectedKind?.Value, SelectedStatus?.Value);
        _filtered = filter.IsEmpty ? _all : _all.Where(filter.Matches).ToList();
        ShowPage(1);
    }

    void ShowPage(int page)
    {
        PageCount = Math.Max(1, (_filtered.Count + PageSize - 1) / PageSize);
        CurrentPage = Math.Clamp(page, 1, PageCount);
        PageLinks = PageLink.Build(CurrentPage, PageCount);
        Products = _filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }
}
