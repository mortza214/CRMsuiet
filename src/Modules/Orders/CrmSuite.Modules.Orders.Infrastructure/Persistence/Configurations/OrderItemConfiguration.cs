using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Orders.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : BaseEntityConfiguration<OrderItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.Property(item => item.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.HasIndex(item => item.OrderId)
            .HasDatabaseName("ix_order_items_order_id");

        builder.Property(item => item.ServiceId).HasColumnName("service_id");
        builder.Property(item => item.DataCenterItemId).HasColumnName("data_center_item_id");
        builder.Property(item => item.AddonId).HasColumnName("addon_id");

        builder.Property(item => item.ItemName)
            .HasColumnName("item_name")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(item => item.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.TotalPrice)
            .HasColumnName("total_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.BillingCycle)
            .HasColumnName("billing_cycle")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(item => item.QuantityDecimal)
            .HasColumnName("quantity_decimal")
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(item => item.UnitPriceSnapshot)
            .HasColumnName("unit_price_snapshot")
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(item => item.AddonsTotal)
            .HasColumnName("addons_total")
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(item => item.AddonsJson)
            .HasColumnName("addons_json")
            .HasColumnType("longtext");
        builder.Property(item => item.AttributesJson)
            .HasColumnName("attributes_json")
            .HasColumnType("longtext");
        builder.Property(item => item.OverridePrice)
            .HasColumnName("override_price")
            .HasPrecision(18, 2);
        builder.Property(item => item.OverrideReason)
            .HasColumnName("override_reason")
            .HasMaxLength(500);
        builder.Property(item => item.OverrideBy)
            .HasColumnName("override_by");
    }
}
