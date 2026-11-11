using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Payments.Infrastructure.Persistence.Configurations;

public sealed class PaymentGatewayTransactionConfiguration
    : BaseEntityConfiguration<PaymentGatewayTransaction>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PaymentGatewayTransaction> builder)
    {
        builder.ToTable("payment_gateway_transactions");

        builder.Property(log => log.TransactionId).HasColumnName("transaction_id");
        builder.HasIndex(log => log.TransactionId).HasDatabaseName("ix_gateway_logs_transaction_id");

        builder.Property(log => log.GatewayName)
            .HasColumnName("gateway_name")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(log => log.Token).HasColumnName("token").HasMaxLength(200);

        builder.Property(log => log.RequestPayload)
            .HasColumnName("request_payload")
            .HasColumnType("longtext")
            .IsRequired();

        builder.Property(log => log.ResponsePayload)
            .HasColumnName("response_payload")
            .HasColumnType("longtext");

        builder.Property(log => log.IsSuccessful)
            .HasColumnName("is_successful")
            .IsRequired();
    }
}
