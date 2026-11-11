using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Configurations;

public sealed class CommissionEntryConfiguration : BaseEntityConfiguration<CommissionEntry>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CommissionEntry> builder)
    {
        builder.ToTable("commission_entries");

        builder.Property(entry => entry.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(entry => entry.OrderItemId).HasColumnName("order_item_id").IsRequired();
        builder.Property(entry => entry.EmployeeId).HasColumnName("employee_id").IsRequired();

        builder.Property(entry => entry.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(entry => entry.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(entry => entry.AdjustmentAmount)
            .HasColumnName("adjustment_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(entry => entry.PayoutType)
            .HasColumnName("payout_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(entry => entry.CustomerId).HasColumnName("customer_id");
        builder.Property(entry => entry.CustomerServiceId).HasColumnName("customer_service_id");
        builder.Property(entry => entry.CustomerName).HasColumnName("customer_name").HasMaxLength(200);
        builder.Property(entry => entry.ServiceName).HasColumnName("service_name").HasMaxLength(200);
        builder.Property(entry => entry.CommissionBase)
            .HasColumnName("commission_base")
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(entry => entry.PaidInstallments).HasColumnName("paid_installments").IsRequired();
        builder.Property(entry => entry.LastPaidAt).HasColumnName("last_paid_at");

        builder.HasMany(entry => entry.Shares)
            .WithOne()
            .HasForeignKey(share => share.CommissionEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entry => new { entry.OrderId, entry.EmployeeId })
            .HasDatabaseName("ix_commission_entries_order_employee");

        builder.HasIndex(entry => new { entry.EmployeeId, entry.Status })
            .HasDatabaseName("ix_commission_entries_employee_status");

        builder.HasMany(entry => entry.Schedules)
            .WithOne()
            .HasForeignKey(schedule => schedule.CommissionEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
