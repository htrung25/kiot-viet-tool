using KiotVietTool.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiotVietTool.Infrastructure.Persistence.Configurations;

internal sealed class TelegramConnectionConfiguration : IEntityTypeConfiguration<TelegramConnection>
{
    public void Configure(EntityTypeBuilder<TelegramConnection> builder)
    {
        builder.ToTable("TelegramConnections");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.EncryptedBotToken).HasMaxLength(2000).IsRequired();
        builder.Property(c => c.BotUsername).HasMaxLength(TelegramConnection.NameMaxLength).IsRequired();
        builder.Property(c => c.ChatTitle).HasMaxLength(TelegramConnection.NameMaxLength).IsRequired();
    }
}
