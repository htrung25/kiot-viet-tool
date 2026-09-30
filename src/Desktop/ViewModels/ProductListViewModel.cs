using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Common.Pagination;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Models;
using KiotVietTool.Desktop.Services;
using KiotVietTool.Domain.Enums;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class ProductListViewModel(
    ICatalogService catalog,
    ICatalogSyncService sync,
    IKiotVietConnectionService connections,
    INavigationService navigation,
    IDialogService dialogs,
    INotificationService notifications) : ViewModelBase
{
    static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(250);
    static readonly FilterOption<int?> AllCategories = new("Tất cả nhóm hàng", null);

    bool _filterSuspended = true;
    CancellationTokenSource? _pendingLoad;

    public IReadOnlyList<FilterOption<int>> PageSizeOptions { get; } =
    [
        new("20 / trang", 20),
        new("50 / trang", 50),
        new("100 / trang", 100),
    ];

    public IReadOnlyList<FilterOption<ProductEnum?>> KindOptions { get; } =
    [
        new("Tất cả loại hàng", null),
        new("Hàng hoá", ProductEnum.Goods),
        new("Combo", ProductEnum.Combo),
        new("Dịch vụ", ProductEnum.Service),
    ];

    public IReadOnlyList<FilterOption<bool?>> StatusOptions { get; } =
    [
        new("Tất cả trạng thái", null),
        new("Đang kinh doanh", true),
        new("Ngừng kinh doanh", false),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProducts), nameof(ShowNoResults), nameof(ShowEmptyState))]
    public partial IReadOnlyList<ProductItem> Products { get; private set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<FilterOption<int?>> CategoryOptions { get; private set; } = [AllCategories];

    [ObservableProperty] public partial string? Keyword { get; set; }
    [ObservableProperty] public partial FilterOption<int?>? SelectedCategory { get; set; } = AllCategories;
    [ObservableProperty] public partial FilterOption<ProductEnum?>? SelectedKind { get; set; }
    [ObservableProperty] public partial FilterOption<bool?>? SelectedStatus { get; set; }
    [ObservableProperty] public partial FilterOption<int>? SelectedPageSize { get; set; }

    [ObservableProperty] public partial string Summary { get; private set; } = "0 sản phẩm";
    [ObservableProperty] public partial int CurrentPage { get; private set; } = 1;
    [ObservableProperty] public partial int PageCount { get; private set; } = 1;
    [ObservableProperty] public partial IReadOnlyList<PageLink> PageLinks { get; private set; } = PageLink.Build(1, 1);
    [ObservableProperty] public partial string? SyncProgressText { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState), nameof(ShowNoResults))]
    public partial bool HasData { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SyncHint))]
    [NotifyCanExecuteChangedFor(nameof(SyncCommand))]
    public partial bool IsConnected { get; private set; }

    public bool HasProducts => Products.Count > 0;
    public bool ShowEmptyState => !HasData && !HasProducts;
    public bool ShowNoResults => HasData && !HasProducts;
    public string SyncHint => IsConnected ? "Tải thay đổi mới nhất từ KiotViet" : "Cần kết nối KiotViet trước khi đồng bộ";

    int PageSize => SelectedPageSize?.Value ?? PageSizeOptions[0].Value;

    public override async Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        SelectedKind = KindOptions[0];
        SelectedStatus = StatusOptions[0];
        SelectedPageSize = PageSizeOptions[0];
        IsConnected = await connections.GetAsync(cancellationToken) is not null;
        await ReloadCategoriesAsync(cancellationToken);
        _filterSuspended = false;
        await LoadPageAsync(1);
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    async Task SyncAsync(CancellationToken cancellationToken)
    {
        var progress = new Progress<SyncProgressDto>(p => SyncProgressText = p.Total is { } total
            ? $"{p.Stage}: {DisplayFormat.Number(p.Done)} / {DisplayFormat.Number(total)}"
            : $"{p.Stage}…");
        SyncProgressText = "Đang đồng bộ…";
        try
        {
            var result = await sync.SyncAsync(progress, cancellationToken);
            if (!result.IsSuccess)
            {
                await dialogs.ShowErrorAsync(result.Error!);
                return;
            }
            notifications.ShowSuccess($"Đã đồng bộ {DisplayFormat.Number(result.Value!.ChangedProducts)} sản phẩm thay đổi.");
        }
        finally
        {
            SyncProgressText = null;
        }

        await ReloadCategoriesAsync(cancellationToken);
        await LoadPageAsync(CurrentPage);
    }

    [RelayCommand]
    Task OpenConnectionAsync(CancellationToken cancellationToken) =>
        navigation.NavigateToAsync<KiotVietConnectionViewModel>(null, cancellationToken);

    [RelayCommand]
    Task ClearFiltersAsync()
    {
        _filterSuspended = true;
        Keyword = "";
        SelectedCategory = AllCategories;
        SelectedKind = KindOptions[0];
        SelectedStatus = StatusOptions[0];
        _filterSuspended = false;
        return LoadPageAsync(1);
    }

    async partial void OnKeywordChanged(string? value)
    {
        if (_filterSuspended) return;
        await Task.Delay(SearchDelay);
        if (Keyword == value) await LoadPageAsync(1);
    }

    partial void OnSelectedCategoryChanged(FilterOption<int?>? value) => ReloadFirstPage();
    partial void OnSelectedKindChanged(FilterOption<ProductEnum?>? value) => ReloadFirstPage();
    partial void OnSelectedStatusChanged(FilterOption<bool?>? value) => ReloadFirstPage();
    partial void OnSelectedPageSizeChanged(FilterOption<int>? value) => ReloadFirstPage();

    [RelayCommand]
    Task GoToPageAsync(int? page) => page is { } number ? LoadPageAsync(number) : Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    Task PreviousPageAsync() => LoadPageAsync(CurrentPage - 1);

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    Task NextPageAsync() => LoadPageAsync(CurrentPage + 1);

    bool CanGoToPreviousPage() => CurrentPage > 1;
    bool CanGoToNextPage() => CurrentPage < PageCount;

    async void ReloadFirstPage()
    {
        if (!_filterSuspended) await LoadPageAsync(1);
    }

    async Task ReloadCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await catalog.GetCategoriesAsync(cancellationToken);
        var selectedId = SelectedCategory?.Value;
        var wasSuspended = _filterSuspended;
        _filterSuspended = true;
        CategoryOptions = [AllCategories, .. CategoryTree.Flatten(categories)
            .Select(x => new FilterOption<int?>(new string(' ', x.Depth * 4) + x.Category.Name, x.Category.Id))];
        SelectedCategory = CategoryOptions.FirstOrDefault(o => o.Value == selectedId) ?? AllCategories;
        _filterSuspended = wasSuspended;
    }

    async Task LoadPageAsync(int page)
    {
        _pendingLoad?.Cancel();
        var cts = _pendingLoad = new CancellationTokenSource();
        var filter = new ProductFilterDto(Keyword, SelectedCategory?.Value, SelectedKind?.Value, SelectedStatus?.Value);
        var request = new PageRequest { Page = page, PageSize = PageSize };

        PagedResult<ProductDto> result;
        try { result = await catalog.GetProductsAsync(filter, request, cts.Token); }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { return; }

        if (result.Items.Count == 0 && result.Page > 1 && result.TotalItems > 0)
        {
            await LoadPageAsync(result.TotalPages ?? 1);
            return;
        }

        var filterIsEmpty = string.IsNullOrWhiteSpace(filter.Keyword) && filter.CategoryId is null && filter.Type is null && filter.IsActive is null;
        if (filterIsEmpty) HasData = result.TotalItems > 0;
        else if (result.Items.Count > 0) HasData = true;

        Products = [.. result.Items.Select(ProductItem.From)];
        CurrentPage = result.Page;
        PageCount = result.TotalPages ?? 1;
        PageLinks = PageLink.Build(CurrentPage, PageCount);
        Summary = result.TotalItems is 0 or null
            ? "0 sản phẩm"
            : $"{DisplayFormat.Number(result.From)}–{DisplayFormat.Number(result.To)} / {DisplayFormat.Number(result.TotalItems.Value)} sản phẩm";
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }
}
