using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Orders.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : BaseEntityConfiguration<Order>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.Property(order => order.OrderNumber)
            .HasColumnName("order_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(order => order.OrderNumber)
            .IsUnique()
            .HasDatabaseName("ux_orders_order_number");

        builder.Property(order => order.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.HasIndex(order => order.CustomerId)
            .HasDatabaseName("ix_orders_customer_id");

        builder.Property(order => order.SalesExpertId)
            .HasColumnName("sales_expert_id")
            .IsRequired();

        builder.HasIndex(order => order.SalesExpertId)
            .HasDatabaseName("ix_orders_sales_expert_id");

        builder.Property(order => order.OrderDate)
            .HasColumnName("order_date")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(order => order.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(order => new { order.Status, order.OrderDate })
            .HasDatabaseName("ix_orders_status_order_date");

        builder.Property(order => order.OrderType)
            .HasColumnName("order_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(order => order.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(order => order.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(order => order.PayableAmount)
            .HasColumnName("payable_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(order => order.Notes)
            .HasColumnName("notes")
            .HasMaxLength(1000);

        builder.Property(order => order.WorkflowInstanceId)
            .HasColumnName("workflow_instance_id");

        builder.Property(order => order.ActivatedAt)
            .HasColumnName("activated_at")
            .HasColumnType("datetime(6)");

        builder.Property(order => order.ActivatedBy)
            .HasColumnName("activated_by");

        builder.HasIndex(order => order.WorkflowInstanceId)
            .HasDatabaseName("ix_orders_workflow_instance_id");

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(order => order.StatusHistory)
            .WithOne()
            .HasForeignKey(history => history.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
