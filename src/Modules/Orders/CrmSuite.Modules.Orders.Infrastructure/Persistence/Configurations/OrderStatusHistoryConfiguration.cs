using CrmSuite.Modules.Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Orders.Infrastructure.Persistence.Configurations;

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_histories");

        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(history => history.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.HasIndex(history => new { history.OrderId, history.ChangedAt })
            .HasDatabaseName("ix_order_status_histories_order_changed");

        builder.Property(history => history.FromStatus)
            .HasColumnName("from_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(history => history.ToStatus)
            .HasColumnName("to_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(history => history.ChangedByUserId)
            .HasColumnName("changed_by_user_id");

        builder.Property(history => history.ChangedAt)
            .HasColumnName("changed_at")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(history => history.Note)
            .HasColumnName("note")
            .HasMaxLength(1000);
    }
}
