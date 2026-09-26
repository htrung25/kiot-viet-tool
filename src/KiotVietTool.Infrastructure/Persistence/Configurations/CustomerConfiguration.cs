using KiotVietTool.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiotVietTool.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        // NOCASE: "kh001" and "KH001" are the same code (ASCII only, SQLite limitation).
        builder.Property(c => c.Code).HasMaxLength(Customer.CodeMaxLength).UseCollation("NOCASE").IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.Name).HasMaxLength(Customer.NameMaxLength).UseCollation("NOCASE").IsRequired();
        builder.HasIndex(c => c.Name);

        builder.Property(c => c.Phone).HasMaxLength(Customer.PhoneMaxLength);
        builder.Property(c => c.Email).HasMaxLength(Customer.EmailMaxLength);
        builder.Property(c => c.Address).HasMaxLength(Customer.AddressMaxLength);
    }
}
