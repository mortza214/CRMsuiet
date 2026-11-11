using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Orders.Infrastructure.Persistence.Configurations;

public sealed class CustomerServiceConfiguration : BaseEntityConfiguration<CustomerService>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CustomerService> builder)
    {
        builder.ToTable("customer_services");

        builder.Property(service => service.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(service => service.ServiceId).HasColumnName("service_id").IsRequired();
        builder.Property(service => service.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(service => service.OrderItemId).HasColumnName("order_item_id").IsRequired();
        builder.Property(service => service.ServiceName).HasColumnName("service_name").HasMaxLength(200).IsRequired();
        builder.Property(service => service.UnitPriceSnapshot).HasColumnName("unit_price_snapshot").HasPrecision(18, 2).IsRequired();
        builder.Property(service => service.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(service => service.AddonsTotal).HasColumnName("addons_total").HasPrecision(18, 2).IsRequired();
        builder.Property(service => service.AttributesJson).HasColumnName("attributes_json").HasColumnType("longtext");
        builder.Property(service => service.CommissionBase).HasColumnName("commission_base").HasPrecision(18, 2).IsRequired();
        builder.Property(service => service.CommissionPercent).HasColumnName("commission_percent").HasPrecision(5, 2);
        builder.Property(service => service.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(service => service.ActivatedAt).HasColumnName("activated_at").HasColumnType("datetime(6)").IsRequired();
        builder.Property(service => service.NextInvoiceAt).HasColumnName("next_invoice_at").HasColumnType("datetime(6)");
        builder.Property(service => service.LastInvoicedAt).HasColumnName("last_invoiced_at").HasColumnType("datetime(6)");
        builder.Property(service => service.IsEquipmentConsigned).HasColumnName("is_equipment_consigned").IsRequired();

        builder.HasIndex(service => new { service.CustomerId, service.Status })
            .HasDatabaseName("ix_customer_services_customer_status");
        builder.HasIndex(service => new { service.Status, service.NextInvoiceAt })
            .HasDatabaseName("ix_customer_services_status_next_invoice");
        builder.HasIndex(service => service.OrderId)
            .IsUnique()
            .HasDatabaseName("ux_customer_services_order_id");
    }
}
