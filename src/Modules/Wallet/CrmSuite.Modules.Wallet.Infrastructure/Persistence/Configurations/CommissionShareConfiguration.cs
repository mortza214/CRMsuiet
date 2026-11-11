using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Configurations;

public sealed class CommissionShareConfiguration : BaseEntityConfiguration<CommissionShare>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CommissionShare> builder)
    {
        builder.ToTable("commission_shares");

        builder.Property(share => share.CommissionEntryId)
            .HasColumnName("commission_entry_id")
            .IsRequired();
        builder.Property(share => share.UserId).HasColumnName("user_id");
        builder.Property(share => share.Percentage)
            .HasColumnName("percentage")
            .HasPrecision(8, 2)
            .IsRequired();
        builder.Property(share => share.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(share => share.IsCompanyShare)
            .HasColumnName("is_company_share")
            .IsRequired();

        builder.HasIndex(share => share.CommissionEntryId)
            .HasDatabaseName("ix_commission_shares_entry");
        builder.HasIndex(share => share.UserId)
            .HasDatabaseName("ix_commission_shares_user");
    }
}
