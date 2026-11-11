using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Notifications.Infrastructure.Persistence.Configurations;

public sealed class NotificationTemplateConfiguration
    : BaseEntityConfiguration<NotificationTemplate>
{
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected override void ConfigureEntity(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");

        builder.Property(template => template.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(template => template.Code)
            .IsUnique()
            .HasDatabaseName("ux_notification_templates_code");

        builder.Property(template => template.Title)
            .HasColumnName("title")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(template => template.Body)
            .HasColumnName("body")
            .HasColumnType("longtext")
            .IsRequired();

        builder.HasData(
            new
            {
                Id = Guid.Parse("61111111-1111-1111-1111-111111111111"),
                Code = "TicketCreated",
                Title = "تیکت جدید",
                Body = "تیکت {TicketNumber} برای {CustomerName} با موضوع {Subject} ثبت شد.",
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("62222222-2222-2222-2222-222222222222"),
                Code = "InvoicePaid",
                Title = "پرداخت موفق",
                Body = "فاکتور {InvoiceNumber} به مبلغ {Amount} تومان پرداخت شد. متشکریم، {CustomerName}.",
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("63333333-3333-3333-3333-333333333333"),
                Code = "InvoiceIssued",
                Title = "فاکتور جدید",
                Body = "فاکتور {InvoiceNumber} به مبلغ {Amount} تومان برای {CustomerName} صادر شد.",
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            });
    }
}
