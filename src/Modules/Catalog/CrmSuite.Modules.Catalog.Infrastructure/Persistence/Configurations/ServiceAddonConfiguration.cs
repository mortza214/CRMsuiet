using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class ServiceAddonConfiguration : BaseEntityConfiguration<ServiceAddon>
{
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected override void ConfigureEntity(EntityTypeBuilder<ServiceAddon> builder)
    {
        builder.ToTable("service_addons");

        builder.Property(addon => addon.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(addon => addon.Name)
            .IsUnique()
            .HasDatabaseName("ux_service_addons_name");

        builder.Property(addon => addon.Price)
            .HasColumnName("price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(addon => addon.Unit)
            .HasColumnName("unit")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(addon => addon.IsOptional)
            .HasColumnName("is_optional")
            .IsRequired();

        builder.Property(addon => addon.ServiceId)
            .HasColumnName("service_id");
        builder.Property(addon => addon.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(addon => addon.Code)
            .HasDatabaseName("ix_service_addons_code");
        builder.Property(addon => addon.PricingMode)
            .HasColumnName("pricing_mode")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(addon => addon.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
        builder.Property(addon => addon.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();
        builder.Property(addon => addon.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);
        builder.HasIndex(addon => new { addon.ServiceId, addon.IsActive })
            .HasDatabaseName("ix_service_addons_service_active");

        builder.HasData(
            new
            {
                Id = Guid.Parse("f1111111-1111-1111-1111-111111111111"),
                Name = "نصب دکل",
                Price = 2000000m,
                Unit = ServiceUnit.Piece,
                IsOptional = true,
                ServiceId = (Guid?)null,
                Code = "SEED-ADDON-2",
                PricingMode = AddonPricingMode.Fixed,
                IsActive = true,
                SortOrder = 0,
                Description = (string?)null,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("f2222222-2222-2222-2222-222222222222"),
                Name = "کابل‌کشی",
                Price = 500000m,
                Unit = ServiceUnit.Piece,
                IsOptional = true,
                ServiceId = (Guid?)null,
                Code = "SEED-ADDON-1",
                PricingMode = AddonPricingMode.Fixed,
                IsActive = true,
                SortOrder = 0,
                Description = (string?)null,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            });
    }
}
