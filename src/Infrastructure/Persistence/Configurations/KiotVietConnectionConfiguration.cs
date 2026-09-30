using KiotVietTool.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiotVietTool.Infrastructure.Persistence.Configurations;

internal sealed class KiotVietConnectionConfiguration : IEntityTypeConfiguration<KiotVietConnection>
{
    public void Configure(EntityTypeBuilder<KiotVietConnection> builder)
    {
        builder.ToTable("KiotVietConnections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Retailer).HasMaxLength(KiotVietConnection.RetailerMaxLength).IsRequired();
        builder.Property(c => c.ClientId).HasMaxLength(KiotVietConnection.ClientIdMaxLength).IsRequired();
        builder.Property(c => c.EncryptedClientSecret).HasMaxLength(2000).IsRequired();
    }
}
