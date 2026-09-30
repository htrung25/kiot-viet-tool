using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Common.Pagination;
using KiotVietTool.Application.DTOs;
using KiotVietTool.Application.Interfaces;
using KiotVietTool.Desktop.Models;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class ProductPickerViewModel(ICatalogService catalog, string placeholder, string emptyText) : ObservableObject
{
    const int MaxResults = 8;
    static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(250);

    public string Placeholder { get; } = placeholder;
    public string EmptyText { get; } = emptyText;
    public ObservableCollection<ProductChoice> Selected { get; } = [];

    [ObservableProperty] public partial string? Keyword { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    public partial IReadOnlyList<ProductChoice> Results { get; private set; } = [];

    public bool HasResults => Results.Count > 0;
    public bool HasSelection => Selected.Count > 0;
    public IReadOnlyList<long> SelectedIds => [.. Selected.Select(p => p.Id)];

    public async Task LoadAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        Selected.Clear();
        foreach (var product in await catalog.GetProductsByIdsAsync(ids, cancellationToken))
            Selected.Add(ProductChoice.From(product));
        OnPropertyChanged(nameof(HasSelection));
    }

    async partial void OnKeywordChanged(string? value)
    {
        await Task.Delay(SearchDelay);
        if (Keyword != value) return;
        if (string.IsNullOrWhiteSpace(value))
        {
            Results = [];
            return;
        }

        var page = await catalog.GetProductsAsync(new ProductFilterDto(value), new PageRequest { PageSize = MaxResults + Selected.Count });
        if (Keyword != value) return;
        var selectedIds = Selected.Select(p => p.Id).ToHashSet();
        Results = [.. page.Items.Where(p => !selectedIds.Contains(p.Id)).Take(MaxResults).Select(ProductChoice.From)];
    }

    [RelayCommand]
    void Add(ProductChoice product)
    {
        if (Selected.All(p => p.Id != product.Id)) Selected.Add(product);
        Results = [.. Results.Where(p => p.Id != product.Id)];
        OnPropertyChanged(nameof(HasSelection));
    }

    [RelayCommand]
    void Remove(ProductChoice product)
    {
        Selected.Remove(product);
        OnPropertyChanged(nameof(HasSelection));
    }
}
