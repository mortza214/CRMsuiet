using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Billing.Infrastructure.Persistence.Configurations;

public sealed class InvoiceItemConfiguration : BaseEntityConfiguration<InvoiceItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("invoice_items");

        builder.Property(item => item.InvoiceId).HasColumnName("invoice_id").IsRequired();

        builder.Property(item => item.ItemName)
            .HasColumnName("item_name")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(item => item.Quantity).HasColumnName("quantity").IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.TotalPrice)
            .HasColumnName("total_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(item => item.InvoiceId)
            .HasDatabaseName("ix_invoice_items_invoice_id");
    }
}
