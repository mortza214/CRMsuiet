using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Wallet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Wallet.Infrastructure.Persistence.Configurations;

public sealed class CommissionRuleConfiguration : BaseEntityConfiguration<CommissionRule>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CommissionRule> builder)
    {
        builder.ToTable("commission_rules");

        builder.Property(rule => rule.ServiceId).HasColumnName("service_id");
        builder.Property(rule => rule.ServiceType)
            .HasColumnName("service_type")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(rule => rule.Percentage)
            .HasColumnName("percentage")
            .HasPrecision(8, 2)
            .IsRequired();

        builder.Property(rule => rule.FixedAmount)
            .HasColumnName("fixed_amount")
            .HasPrecision(18, 2);

        builder.Property(rule => rule.PayoutType)
            .HasColumnName("payout_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(rule => new { rule.ServiceId, rule.ServiceType })
            .HasDatabaseName("ix_commission_rules_target");
    }
}
