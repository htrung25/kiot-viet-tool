using KiotVietTool.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiotVietTool.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Code).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(500).IsRequired();
        builder.Property(p => p.FullName).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Unit).HasMaxLength(100).IsRequired();
        builder.Property(p => p.SearchKey).HasMaxLength(700).IsRequired();
        builder.Property(p => p.BasePrice).HasConversion<long>();
        builder.Property(p => p.Type).HasConversion<int>();
        builder.HasIndex(p => p.Code);
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.MasterUnitId);
    }
}
