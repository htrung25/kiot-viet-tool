using KiotVietTool.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiotVietTool.Infrastructure.Persistence.Configurations;

internal sealed class ProgramProductPriceConfiguration : IEntityTypeConfiguration<ProgramProductPrice>
{
    public void Configure(EntityTypeBuilder<ProgramProductPrice> builder)
    {
        builder.ToTable("ProgramProductPrices");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.ProductCode).HasMaxLength(100).IsRequired();
        builder.Property(p => p.ProductName).HasMaxLength(500).IsRequired();
        builder.Property(p => p.OriginalPrice).HasConversion<long>();
        builder.Property(p => p.DiscountedPrice).HasConversion<long>();
        builder.Property(p => p.State).HasConversion<int>();
        builder.Property(p => p.LastError).HasMaxLength(ProgramProductPrice.ErrorMaxLength);
        builder.HasIndex(p => new { p.ProgramId, p.ProductId }).IsUnique();
        builder.HasIndex(p => p.ProductId);
        builder.HasOne<DiscountProgram>().WithMany().HasForeignKey(p => p.ProgramId).OnDelete(DeleteBehavior.Cascade);
    }
}
