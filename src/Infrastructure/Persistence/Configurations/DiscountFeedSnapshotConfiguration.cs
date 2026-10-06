using KiotVietTool.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiotVietTool.Infrastructure.Persistence.Configurations;

internal sealed class DiscountFeedSnapshotConfiguration : IEntityTypeConfiguration<DiscountFeedSnapshot>
{
    public void Configure(EntityTypeBuilder<DiscountFeedSnapshot> builder)
    {
        builder.ToTable("DiscountFeedSnapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ProgramsJson).IsRequired();
        builder.Ignore(s => s.Programs);
        builder.HasIndex(s => s.PublishedAtUtc);
    }
}
