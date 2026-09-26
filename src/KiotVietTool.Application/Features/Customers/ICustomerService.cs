using KiotVietTool.Application.Common;

namespace KiotVietTool.Application.Features.Customers;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> SearchAsync(string? keyword, CancellationToken cancellationToken = default);
    Task<Result<CustomerDto>> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<CustomerDto>> CreateAsync(SaveCustomerRequest request, CancellationToken cancellationToken = default);
    Task<Result<CustomerDto>> UpdateAsync(int id, SaveCustomerRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
