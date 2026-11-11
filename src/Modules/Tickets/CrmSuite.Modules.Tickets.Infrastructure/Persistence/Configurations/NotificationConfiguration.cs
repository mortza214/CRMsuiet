using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Tickets.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : BaseEntityConfiguration<Notification>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.Property(notification => notification.DepartmentId).HasColumnName("department_id");
        builder.Property(notification => notification.UserId).HasColumnName("user_id");
        builder.Property(notification => notification.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();
        builder.Property(notification => notification.Body)
            .HasColumnName("body")
            .HasColumnType("longtext")
            .IsRequired();
        builder.Property(notification => notification.IsRead)
            .HasColumnName("is_read")
            .IsRequired();

        builder.HasIndex(notification => new { notification.DepartmentId, notification.IsRead })
            .HasDatabaseName("ix_notifications_department_read");
    }
}
