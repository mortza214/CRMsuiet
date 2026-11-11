using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Tickets.Infrastructure.Persistence.Configurations;

public sealed class TicketMessageConfiguration : BaseEntityConfiguration<TicketMessage>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TicketMessage> builder)
    {
        builder.ToTable("ticket_messages");

        builder.Property(message => message.TicketId).HasColumnName("ticket_id").IsRequired();
        builder.Property(message => message.SenderId).HasColumnName("sender_id").IsRequired();

        builder.Property(message => message.SenderType)
            .HasColumnName("sender_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(message => message.Message)
            .HasColumnName("message")
            .HasColumnType("longtext")
            .IsRequired();

        builder.Property(message => message.IsInternalNote)
            .HasColumnName("is_internal_note")
            .IsRequired();

        builder.HasIndex(message => new { message.TicketId, message.CreatedAt })
            .HasDatabaseName("ix_ticket_messages_ticket_created");
    }
}
