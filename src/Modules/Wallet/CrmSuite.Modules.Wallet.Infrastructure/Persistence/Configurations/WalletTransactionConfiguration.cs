using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Configurations;

public sealed class WalletTransactionConfiguration : BaseEntityConfiguration<WalletTransaction>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("wallet_transactions");

        builder.Property(transaction => transaction.WalletId).HasColumnName("wallet_id").IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transaction => transaction.TransactionType)
            .HasColumnName("transaction_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(transaction => transaction.ReferenceId).HasColumnName("reference_id");
        builder.Property(transaction => transaction.Description).HasColumnName("description").HasMaxLength(500);

        builder.Property(transaction => transaction.BalanceAfter)
            .HasColumnName("balance_after")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(transaction => transaction.WalletId)
            .HasDatabaseName("ix_wallet_transactions_wallet_id");
    }
}
