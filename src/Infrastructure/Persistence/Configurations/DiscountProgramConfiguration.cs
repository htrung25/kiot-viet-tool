using KiotVietTool.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiotVietTool.Infrastructure.Persistence.Configurations;

internal sealed class DiscountProgramConfiguration : IEntityTypeConfiguration<DiscountProgram>
{
    public void Configure(EntityTypeBuilder<DiscountProgram> builder)
    {
        builder.ToTable("DiscountPrograms");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(DiscountProgram.NameMaxLength).IsRequired();
        builder.Property(p => p.Note).HasMaxLength(DiscountProgram.NoteMaxLength);
        builder.Property(p => p.Value).HasConversion<string>();
        builder.Property(p => p.Type).HasConversion<int>();
        builder.Property(p => p.Rounding).HasConversion<int>();
        builder.Property(p => p.Scope).HasConversion<int>();
        builder.Property(p => p.UnitScope).HasConversion<int>();
        builder.Property(p => p.Status).HasConversion<int>();
        builder.Property(p => p.StartMode).HasConversion<int>();
        builder.PrimitiveCollection(p => p.CategoryIds);
        builder.PrimitiveCollection(p => p.ProductIds);
        builder.PrimitiveCollection(p => p.ExcludedProductIds);
        builder.HasIndex(p => p.Status);
    }
}
