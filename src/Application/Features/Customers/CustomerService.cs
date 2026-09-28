using KiotVietTool.Application.Common;
using KiotVietTool.Domain.Common;
using KiotVietTool.Domain.Customers;

namespace KiotVietTool.Application.Features.Customers;

public sealed class CustomerService(ICustomerRepository repository, TimeProvider timeProvider) : ICustomerService
{
    const string NotFound = "Không tìm thấy khách hàng.";

    public async Task<IReadOnlyList<CustomerDto>> SearchAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        var customers = await repository.SearchAsync(keyword?.Trim(), cancellationToken);
        return [.. customers.Select(CustomerDto.From)];
    }

    public async Task<Result<CustomerDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetByIdAsync(id, cancellationToken);
        return customer is null ? Result.Failure<CustomerDto>(NotFound) : Result.Success(CustomerDto.From(customer));
    }

    public async Task<Result<CustomerDto>> CreateAsync(SaveCustomerRequest request, CancellationToken cancellationToken = default)
    {
        Customer customer;
        try
        {
            customer = Customer.Create(request.Code, request.Name, request.Phone, request.Email, request.Address, UtcNow);
        }
        catch (DomainException ex) { return Result.Failure<CustomerDto>(ex.Message); }

        if (await repository.CodeExistsAsync(customer.Code, null, cancellationToken))
            return DuplicateCode(customer.Code);

        await repository.AddAsync(customer, cancellationToken);
        return Result.Success(CustomerDto.From(customer));
    }

    public async Task<Result<CustomerDto>> UpdateAsync(int id, SaveCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetByIdAsync(id, cancellationToken);
        if (customer is null) return Result.Failure<CustomerDto>(NotFound);

        try
        {
            customer.Update(request.Code, request.Name, request.Phone, request.Email, request.Address, UtcNow);
        }
        catch (DomainException ex) { return Result.Failure<CustomerDto>(ex.Message); }

        if (await repository.CodeExistsAsync(customer.Code, id, cancellationToken))
            return DuplicateCode(customer.Code);

        await repository.UpdateAsync(customer, cancellationToken);
        return Result.Success(CustomerDto.From(customer));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        await repository.DeleteAsync(id, cancellationToken) ? Result.Success() : Result.Failure(NotFound);

    DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    static Result<CustomerDto> DuplicateCode(string code) =>
        Result.Failure<CustomerDto>($"Mã khách hàng \"{code}\" đã tồn tại.");
}
