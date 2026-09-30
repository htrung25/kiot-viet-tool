using KiotVietTool.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiotVietTool.Infrastructure.Persistence.Configurations;

internal sealed class PriceBookConfiguration : IEntityTypeConfiguration<PriceBook>
{
    public void Configure(EntityTypeBuilder<PriceBook> builder)
    {
        builder.ToTable("PriceBooks");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Name).HasMaxLength(255).IsRequired();
        builder.PrimitiveCollection(b => b.BranchIds);
        builder.PrimitiveCollection(b => b.CustomerGroupIds);
        builder.PrimitiveCollection(b => b.UserIds);
    }
}
