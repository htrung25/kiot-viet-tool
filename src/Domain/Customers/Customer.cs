using System.Net.Mail;

using KiotVietTool.Domain.Common;

namespace KiotVietTool.Domain.Customers;

public sealed class Customer
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 200;
    public const int PhoneMaxLength = 20;
    public const int EmailMaxLength = 200;
    public const int AddressMaxLength = 500;

    public int Id { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private Customer() { } // EF Core

    public static Customer Create(string code, string name, string? phone, string? email, string? address, DateTime nowUtc)
    {
        var customer = new Customer { CreatedAtUtc = nowUtc };
        customer.Update(code, name, phone, email, address, nowUtc);
        return customer;
    }

    public void Update(string code, string name, string? phone, string? email, string? address, DateTime nowUtc)
    {
        Code = Required(code, CodeMaxLength, "Mã khách hàng");
        Name = Required(name, NameMaxLength, "Tên khách hàng");
        Phone = Optional(phone, PhoneMaxLength, "Số điện thoại");
        Email = Optional(email, EmailMaxLength, "Email");
        Address = Optional(address, AddressMaxLength, "Địa chỉ");
        if (Email is not null && !MailAddress.TryCreate(Email, out _))
            throw new DomainException("Email không hợp lệ.");
        UpdatedAtUtc = nowUtc;
    }

    static string Required(string? value, int maxLength, string field) =>
        Optional(value, maxLength, field) ?? throw new DomainException($"{field} không được để trống.");

    static string? Optional(string? value, int maxLength, string field)
    {
        value = value?.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        if (value.Length > maxLength) throw new DomainException($"{field} tối đa {maxLength} ký tự.");
        return value;
    }
}
