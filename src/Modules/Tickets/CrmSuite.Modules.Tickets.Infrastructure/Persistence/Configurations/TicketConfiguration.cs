using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Tickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Tickets.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : BaseEntityConfiguration<Ticket>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.Property(ticket => ticket.TicketNumber)
            .HasColumnName("ticket_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(ticket => ticket.TicketNumber)
            .IsUnique()
            .HasDatabaseName("ux_tickets_ticket_number");

        builder.Property(ticket => ticket.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(ticket => ticket.ServiceId).HasColumnName("service_id");
        builder.Property(ticket => ticket.Subject).HasColumnName("subject").HasMaxLength(300).IsRequired();
        builder.Property(ticket => ticket.DepartmentId).HasColumnName("department_id").IsRequired();
        builder.Property(ticket => ticket.AssignedToUserId).HasColumnName("assigned_to_user_id");
        builder.Property(ticket => ticket.WorkflowInstanceId).HasColumnName("workflow_instance_id");
        builder.HasIndex(ticket => ticket.WorkflowInstanceId)
            .HasDatabaseName("ix_tickets_workflow_instance_id");

        builder.Property(ticket => ticket.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ticket => ticket.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ticket => ticket.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(ticket => new { ticket.DepartmentId, ticket.Status })
            .HasDatabaseName("ix_tickets_department_status");

        builder.HasIndex(ticket => ticket.CustomerId)
            .HasDatabaseName("ix_tickets_customer_id");

        builder.HasMany(ticket => ticket.Messages)
            .WithOne()
            .HasForeignKey(message => message.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ticket => ticket.StatusHistory)
            .WithOne()
            .HasForeignKey(history => history.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
