using KiotVietTool.Domain.Customers;

namespace KiotVietTool.Application.Features.Customers;

public sealed record CustomerDto(
    int Id, string Code, string Name, string? Phone, string? Email, string? Address, DateTime UpdatedAtUtc)
{
    internal static CustomerDto From(Customer c) =>
        new(c.Id, c.Code, c.Name, c.Phone, c.Email, c.Address, c.UpdatedAtUtc);
}

public sealed record SaveCustomerRequest(string Code, string Name, string? Phone, string? Email, string? Address);
