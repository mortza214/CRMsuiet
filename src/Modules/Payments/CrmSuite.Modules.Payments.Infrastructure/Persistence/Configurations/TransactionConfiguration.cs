using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Payments.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : BaseEntityConfiguration<Transaction>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.Property(transaction => transaction.InvoiceId).HasColumnName("invoice_id");
        builder.Property(transaction => transaction.CustomerId).HasColumnName("customer_id").IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction => transaction.PaymentMethod)
            .HasColumnName("payment_method")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(transaction => transaction.GatewayName)
            .HasColumnName("gateway_name")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(transaction => transaction.ReferenceNumber)
            .HasColumnName("reference_number")
            .HasMaxLength(200);

        builder.Property(transaction => transaction.TrackingCode)
            .HasColumnName("tracking_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(transaction => transaction.TrackingCode)
            .IsUnique()
            .HasDatabaseName("ux_transactions_tracking_code");

        builder.Property(transaction => transaction.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(transaction => transaction.PaidAt)
            .HasColumnName("paid_at")
            .HasColumnType("datetime(6)");

        builder.Property(transaction => transaction.GatewayResponse)
            .HasColumnName("gateway_response")
            .HasColumnType("longtext");

        builder.HasIndex(transaction => new { transaction.Status, transaction.CreatedAt })
            .HasDatabaseName("ix_transactions_status_created");

        builder.HasIndex(transaction => transaction.CustomerId)
            .HasDatabaseName("ix_transactions_customer_id");

        builder.HasIndex(transaction => transaction.InvoiceId)
            .HasDatabaseName("ix_transactions_invoice_id");
    }
}
