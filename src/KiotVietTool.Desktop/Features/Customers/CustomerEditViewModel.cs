using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using KiotVietTool.Application.Features.Customers;
using KiotVietTool.Desktop.Common;
using KiotVietTool.Desktop.Common.Dialogs;
using KiotVietTool.Desktop.Common.Navigation;

namespace KiotVietTool.Desktop.Features.Customers;

public sealed partial class CustomerEditViewModel(
    ICustomerService customerService,
    INavigationService navigation,
    IDialogService dialog) : ViewModelBase
{
    int? _customerId;

    public string Title => _customerId is null ? "Thêm khách hàng" : "Sửa khách hàng";

    [ObservableProperty] public partial string Code { get; set; } = "";
    [ObservableProperty] public partial string Name { get; set; } = "";
    [ObservableProperty] public partial string? Phone { get; set; }
    [ObservableProperty] public partial string? Email { get; set; }
    [ObservableProperty] public partial string? Address { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    public override async Task OnNavigatedToAsync(object? parameter, CancellationToken cancellationToken)
    {
        if (parameter is not int id) return;

        var result = await customerService.GetAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            await dialog.ShowErrorAsync(result.Error!);
            await BackToListAsync(cancellationToken);
            return;
        }

        var customer = result.Value!;
        _customerId = customer.Id;
        (Code, Name, Phone, Email, Address) = (customer.Code, customer.Name, customer.Phone, customer.Email, customer.Address);
        OnPropertyChanged(nameof(Title));
    }

    [RelayCommand]
    async Task SaveAsync(CancellationToken cancellationToken)
    {
        var request = new SaveCustomerRequest(Code, Name, Phone, Email, Address);
        var result = _customerId is { } id
            ? await customerService.UpdateAsync(id, request, cancellationToken)
            : await customerService.CreateAsync(request, cancellationToken);

        ErrorMessage = result.Error;
        if (result.IsSuccess) await BackToListAsync(cancellationToken);
    }

    [RelayCommand]
    Task CancelAsync(CancellationToken cancellationToken) => BackToListAsync(cancellationToken);

    Task BackToListAsync(CancellationToken cancellationToken) =>
        navigation.NavigateToAsync<CustomerListViewModel>(null, cancellationToken);
}
