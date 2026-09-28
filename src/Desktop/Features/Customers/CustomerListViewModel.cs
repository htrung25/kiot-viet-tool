using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Features.Customers;
using KiotVietTool.Desktop.Common;
using KiotVietTool.Desktop.Common.Dialogs;
using KiotVietTool.Desktop.Common.Navigation;
using KiotVietTool.Desktop.Common.Notifications;

namespace KiotVietTool.Desktop.Features.Customers;

public sealed partial class CustomerListViewModel(
    ICustomerService customerService,
    INavigationService navigation,
    IDialogService dialog,
    INotificationService notifications) : ViewModelBase
{
    static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(300);

    CancellationTokenSource? _pendingSearch;
    string? _searchedKeyword;

    public ObservableCollection<CustomerDto> Customers { get; } = [];

    [ObservableProperty]
    public partial string? Keyword { get; set; }

    [ObservableProperty]
    public partial CustomerDto? SelectedCustomer { get; set; }

    public bool HasCustomers => Customers.Count > 0;
    public bool IsFiltered => !string.IsNullOrWhiteSpace(_searchedKeyword);
    public bool ShowEmptyState => !HasCustomers && !IsFiltered;
    public bool ShowNoResults => !HasCustomers && IsFiltered;
    public string Summary => IsFiltered
        ? $"{Customers.Count} kết quả cho “{_searchedKeyword!.Trim()}”"
        : $"{Customers.Count} khách hàng";

    public override Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken) =>
        SearchAsync(cancellationToken);

    // Search as you type, once typing pauses.
    async partial void OnKeywordChanged(string? value)
    {
        _pendingSearch?.Cancel();
        _pendingSearch = new CancellationTokenSource();
        var token = _pendingSearch.Token;
        try
        {
            await Task.Delay(SearchDelay, token);
            await SearchAsync(token);
        }
        catch (OperationCanceledException) { } // superseded by newer typing
    }

    [RelayCommand]
    async Task SearchAsync(CancellationToken cancellationToken)
    {
        var keyword = Keyword;
        var customers = await customerService.SearchAsync(keyword, cancellationToken);
        Customers.Clear();
        foreach (var customer in customers) Customers.Add(customer);
        _searchedKeyword = keyword;
        OnPropertyChanged(nameof(HasCustomers));
        OnPropertyChanged(nameof(IsFiltered));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(ShowNoResults));
        OnPropertyChanged(nameof(Summary));
    }

    [RelayCommand]
    void ClearSearch() => Keyword = "";

    [RelayCommand]
    Task AddAsync(CancellationToken cancellationToken) =>
        navigation.NavigateToAsync<CustomerEditViewModel>(null, cancellationToken);

    [RelayCommand(CanExecute = nameof(IsCustomer))]
    Task EditAsync(CustomerDto? customer, CancellationToken cancellationToken) =>
        navigation.NavigateToAsync<CustomerEditViewModel>(customer!.Id, cancellationToken);

    [RelayCommand(CanExecute = nameof(IsCustomer))]
    async Task DeleteAsync(CustomerDto? customer, CancellationToken cancellationToken)
    {
        var confirmed = await dialog.ConfirmAsync(
            "Xoá khách hàng?",
            $"Khách hàng “{customer!.Name}” ({customer.Code}) sẽ bị xoá vĩnh viễn. Không thể hoàn tác.",
            "Xoá khách hàng", destructive: true);
        if (!confirmed) return;

        var result = await customerService.DeleteAsync(customer.Id, cancellationToken);
        if (result.IsSuccess) notifications.ShowSuccess($"Đã xoá khách hàng “{customer.Name}”.");
        else await dialog.ShowErrorAsync(result.Error!);
        await SearchAsync(cancellationToken);
    }

    static bool IsCustomer(CustomerDto? customer) => customer is not null;
}
