using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Notifications.Infrastructure.Persistence.Configurations;

public sealed class NotificationLogConfiguration : BaseEntityConfiguration<NotificationLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_logs");

        builder.Property(log => log.Recipient)
            .HasColumnName("recipient")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(log => log.Channel)
            .HasColumnName("channel")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(log => log.TemplateCode)
            .HasColumnName("template_code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(log => log.Content)
            .HasColumnName("content")
            .HasColumnType("longtext")
            .IsRequired();

        builder.Property(log => log.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(log => log.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("longtext");

        builder.Property(log => log.IsRead).HasColumnName("is_read").IsRequired();
        builder.Property(log => log.SentAt).HasColumnName("sent_at").HasColumnType("datetime(6)");

        builder.HasIndex(log => new { log.Recipient, log.Channel, log.IsRead })
            .HasDatabaseName("ix_notification_logs_recipient_channel_read");
    }
}
