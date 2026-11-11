using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Settings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Settings.Infrastructure.Persistence.Configurations;

public sealed class TaxRuleConfiguration : BaseEntityConfiguration<TaxRule>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TaxRule> builder)
    {
        builder.ToTable("tax_rules");

        builder.Property(rule => rule.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(rule => rule.Percentage)
            .HasColumnName("percentage")
            .HasPrecision(8, 2)
            .IsRequired();

        builder.Property(rule => rule.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(rule => rule.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasIndex(rule => new { rule.IsActive, rule.EffectiveFrom })
            .HasDatabaseName("ix_tax_rules_active_effective");
    }
}
