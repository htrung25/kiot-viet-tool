using KiotVietTool.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiotVietTool.Infrastructure.Persistence.Configurations;

internal sealed class PriceBookItemConfiguration : IEntityTypeConfiguration<PriceBookItem>
{
    public void Configure(EntityTypeBuilder<PriceBookItem> builder)
    {
        builder.ToTable("PriceBookItems");
        builder.HasKey(i => new { i.PriceBookId, i.ProductId });
        builder.Property(i => i.Price).HasConversion<long>();
        builder.HasIndex(i => i.ProductId);
    }
}
