using CrmSuite.Modules.Foundation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;

public sealed class SettingConfiguration : BaseEntityConfiguration<Setting>
{
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected override void ConfigureEntity(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("settings");

        builder.Property(setting => setting.Key)
            .HasColumnName("key")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(setting => setting.Key)
            .IsUnique()
            .HasDatabaseName("ux_settings_key");

        builder.Property(setting => setting.Value)
            .HasColumnName("value")
            .HasColumnType("longtext")
            .IsRequired();

        builder.Property(setting => setting.Group)
            .HasColumnName("group_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(setting => setting.IsSecret)
            .HasColumnName("is_secret")
            .IsRequired();

        builder.HasIndex(setting => setting.Group)
            .HasDatabaseName("ix_settings_group_name");

        builder.HasData(
            new
            {
                Id = Guid.Parse("51111111-1111-1111-1111-111111111111"),
                Key = "Billing.VatPercentage",
                Value = "10",
                Group = "Billing",
                IsSecret = false,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("52222222-2222-2222-2222-222222222222"),
                Key = "Billing.InsurancePercentage",
                Value = "0",
                Group = "Billing",
                IsSecret = false,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("53333333-3333-3333-3333-333333333333"),
                Key = "Billing.ContractTemplate",
                Value = "قرارداد {ContractNumber}\nمشتری: {CustomerName}\nاز {StartDate} تا {EndDate}\nسفارش: {OrderNumber}",
                Group = "Billing",
                IsSecret = false,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            });
    }
}
