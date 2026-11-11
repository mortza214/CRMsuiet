using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletEntity = CrmSuite.Modules.Wallet.Domain.Entities.Wallet;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Configurations;

public sealed class WalletConfiguration : BaseEntityConfiguration<WalletEntity>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WalletEntity> builder)
    {
        builder.ToTable("wallets");

        builder.Property(wallet => wallet.OwnerId).HasColumnName("owner_id").IsRequired();
        builder.Property(wallet => wallet.OwnerType)
            .HasColumnName("owner_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(wallet => wallet.Balance)
            .HasColumnName("balance")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(wallet => new { wallet.OwnerId, wallet.OwnerType })
            .IsUnique()
            .HasDatabaseName("ux_wallets_owner");

        builder.HasMany(wallet => wallet.Transactions)
            .WithOne()
            .HasForeignKey(transaction => transaction.WalletId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
