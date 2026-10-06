using KiotVietTool.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiotVietTool.Infrastructure.Persistence.Configurations;

internal sealed class DiscountFeedConnectionConfiguration : IEntityTypeConfiguration<DiscountFeedConnection>
{
    public void Configure(EntityTypeBuilder<DiscountFeedConnection> builder)
    {
        builder.ToTable("DiscountFeedConnections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.WorkerUrl).HasMaxLength(DiscountFeedConnection.UrlMaxLength).IsRequired();
        builder.Property(c => c.EncryptedWriteToken).HasMaxLength(2000).IsRequired();
        builder.Property(c => c.EncryptedReadToken).HasMaxLength(2000);
        builder.Property(c => c.CloudflareAccountId).HasMaxLength(64);
        builder.Property(c => c.ScriptName).HasMaxLength(DiscountFeedConnection.ScriptNameMaxLength);
        builder.Property(c => c.InstanceId).HasMaxLength(64).IsRequired();
    }
}
