using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Tickets.Infrastructure.Persistence.Configurations;

public sealed class TicketStatusHistoryConfiguration
    : BaseEntityConfiguration<TicketStatusHistory>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TicketStatusHistory> builder)
    {
        builder.ToTable("ticket_status_histories");

        builder.Property(history => history.TicketId).HasColumnName("ticket_id").IsRequired();

        builder.Property(history => history.FromStatus)
            .HasColumnName("from_status")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(history => history.ToStatus)
            .HasColumnName("to_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(history => history.ChangedByUserId).HasColumnName("changed_by_user_id");
        builder.Property(history => history.ChangedAt)
            .HasColumnName("changed_at")
            .HasColumnType("datetime(6)")
            .IsRequired();
        builder.Property(history => history.Note).HasColumnName("note").HasMaxLength(1000);
    }
}
