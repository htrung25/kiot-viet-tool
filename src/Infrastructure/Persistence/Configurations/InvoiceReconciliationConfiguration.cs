using KiotVietTool.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiotVietTool.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceReconciliationConfiguration : IEntityTypeConfiguration<InvoiceReconciliation>
{
    public void Configure(EntityTypeBuilder<InvoiceReconciliation> builder)
    {
        builder.ToTable("InvoiceReconciliations");
        builder.HasKey(r => r.InvoiceId);
        builder.Property(r => r.InvoiceId).ValueGeneratedNever();
        builder.Property(r => r.Code).HasMaxLength(100).IsRequired();
        builder.Property(r => r.BranchName).HasMaxLength(255);
        builder.Property(r => r.SoldByName).HasMaxLength(255);
        builder.Property(r => r.ProgramNames).HasMaxLength(1000);
        builder.Property(r => r.Total).HasConversion<string>();
        builder.Property(r => r.ActualDiscount).HasConversion<string>();
        builder.Property(r => r.ExpectedDiscount).HasConversion<string>();
        builder.Property(r => r.Outcome).HasConversion<int>();
        builder.Property(r => r.LinesJson).IsRequired();
        builder.Ignore(r => r.Lines);
        builder.Ignore(r => r.NeedsReview);
        builder.HasIndex(r => r.PurchasedAtUtc);
        builder.HasIndex(r => new { r.IsReviewed, r.Outcome });
    }
}
