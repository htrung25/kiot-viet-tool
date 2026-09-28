using KiotVietTool.Domain.Customers;

namespace KiotVietTool.Application.Features.Customers;

/// <summary>Each call is its own unit of work (desktop app: no long-lived DbContext).</summary>
public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> SearchAsync(string? keyword, CancellationToken cancellationToken);
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken);
    Task AddAsync(Customer customer, CancellationToken cancellationToken);
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
