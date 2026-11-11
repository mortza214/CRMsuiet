using CrmSuite.Modules.Billing.Domain.Entities;
using CrmSuite.Modules.Billing.Domain.Enums;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Billing.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : BaseEntityConfiguration<Invoice>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.Property(invoice => invoice.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(invoice => invoice.InvoiceNumber)
            .IsUnique()
            .HasDatabaseName("ux_invoices_invoice_number");

        builder.Property(invoice => invoice.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(invoice => invoice.CustomerId).HasColumnName("customer_id").IsRequired();

        builder.Property(invoice => invoice.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(invoice => invoice.IssueDate)
            .HasColumnName("issue_date")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(invoice => invoice.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(invoice => invoice.SubTotal)
            .HasColumnName("sub_total")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(invoice => invoice.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(invoice => invoice.TaxAmount)
            .HasColumnName("tax_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(invoice => invoice.InsuranceAmount)
            .HasColumnName("insurance_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(invoice => invoice.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(invoice => invoice.PaidAmount)
            .HasColumnName("paid_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(invoice => invoice.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(invoice => invoice.PaymentMode)
            .HasColumnName("payment_mode")
            .HasMaxLength(30);

        builder.Property(invoice => invoice.RequiresPayment)
            .HasColumnName("requires_payment")
            .IsRequired();

        builder.HasIndex(invoice => new { invoice.OrderId, invoice.Type })
            .HasDatabaseName("ix_invoices_order_type");

        builder.HasIndex(invoice => new { invoice.Status, invoice.IssueDate })
            .HasDatabaseName("ix_invoices_status_issue_date");

        builder.HasMany(invoice => invoice.Items)
            .WithOne()
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
