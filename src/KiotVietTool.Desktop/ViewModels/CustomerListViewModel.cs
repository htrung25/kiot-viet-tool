using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KiotVietTool.Application.Features.Customers;
using KiotVietTool.Desktop.Services;

namespace KiotVietTool.Desktop.ViewModels;

public sealed partial class CustomerListViewModel(
    ICustomerService customerService,
    INavigationService navigation,
    IDialogService dialog) : ViewModelBase
{
    public ObservableCollection<CustomerDto> Customers { get; } = [];

    [ObservableProperty]
    public partial string? Keyword { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCommand), nameof(DeleteCommand))]
    public partial CustomerDto? SelectedCustomer { get; set; }

    public override Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken) =>
        SearchAsync(cancellationToken);

    [RelayCommand]
    async Task SearchAsync(CancellationToken cancellationToken)
    {
        var customers = await customerService.SearchAsync(Keyword, cancellationToken);
        Customers.Clear();
        foreach (var customer in customers) Customers.Add(customer);
    }

    [RelayCommand]
    Task AddAsync(CancellationToken cancellationToken) =>
        navigation.NavigateToAsync<CustomerEditViewModel>(null, cancellationToken);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    Task EditAsync(CancellationToken cancellationToken) =>
        navigation.NavigateToAsync<CustomerEditViewModel>(SelectedCustomer!.Id, cancellationToken);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    async Task DeleteAsync(CancellationToken cancellationToken)
    {
        var customer = SelectedCustomer!;
        if (!dialog.Confirm($"Xoá khách hàng \"{customer.Name}\" ({customer.Code})?")) return;

        var result = await customerService.DeleteAsync(customer.Id, cancellationToken);
        if (!result.IsSuccess) dialog.ShowError(result.Error!);
        await SearchAsync(cancellationToken);
    }

    bool HasSelection() => SelectedCustomer is not null;
}
