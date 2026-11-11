using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Configurations;

public sealed class CommissionScheduleConfiguration : BaseEntityConfiguration<CommissionSchedule>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CommissionSchedule> builder)
    {
        builder.ToTable("commission_schedules");

        builder.Property(schedule => schedule.CommissionEntryId)
            .HasColumnName("commission_entry_id")
            .IsRequired();

        builder.Property(schedule => schedule.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(schedule => schedule.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(schedule => schedule.IsPaid)
            .HasColumnName("is_paid")
            .IsRequired();

        builder.Property(schedule => schedule.PaidAt)
            .HasColumnName("paid_at")
            .HasColumnType("datetime(6)");

        builder.HasIndex(schedule => new { schedule.DueDate, schedule.IsPaid })
            .HasDatabaseName("ix_commission_schedules_due");
    }
}
