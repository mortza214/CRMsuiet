using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class ServiceConfiguration : BaseEntityConfiguration<Service>
{
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected override void ConfigureEntity(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("services");

        builder.Property(service => service.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(service => service.Code)
            .IsUnique()
            .HasDatabaseName("ux_services_code");

        builder.Property(service => service.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(service => service.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.HasOne(service => service.Category)
            .WithMany()
            .HasForeignKey(service => service.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(service => service.ServiceType)
            .HasColumnName("service_type")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(service => service.Unit)
            .HasColumnName("unit")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(service => service.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(service => service.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(service => service.WorkflowDefinitionId)
            .HasColumnName("workflow_definition_id");

        builder.Property(service => service.PricingMode)
            .HasColumnName("pricing_mode")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(service => service.BillingPeriod)
            .HasColumnName("billing_period")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(service => service.CommissionPercent)
            .HasColumnName("commission_percent")
            .HasPrecision(5, 2);

        builder.Property(service => service.AttributeSchemaJson)
            .HasColumnName("attribute_schema_json")
            .HasColumnType("longtext");

        builder.HasIndex(service => service.WorkflowDefinitionId)
            .HasDatabaseName("ix_services_workflow_definition_id");

        builder.HasIndex(service => service.CategoryId)
            .HasDatabaseName("ix_services_category_id");

        builder.HasIndex(service => new { service.ServiceType, service.IsActive })
            .HasDatabaseName("ix_services_type_active");

        builder.HasMany(service => service.Prices)
            .WithOne()
            .HasForeignKey(price => price.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new
            {
                Id = Guid.Parse("d1111111-1111-1111-1111-111111111111"),
                Code = "BW-DED-10",
                Name = "پهنای باند اختصاصی ۱۰ مگ",
                CategoryId = Guid.Parse("c1111111-1111-1111-1111-111111111111"),
                ServiceType = ServiceType.BandwidthDedicated,
                Unit = ServiceUnit.BandwidthMbps,
                Description = "پهنای باند اختصاصی بر اساس هر مگابیت، مستقل از بستر",
                IsActive = true,
                PricingMode = PricingMode.UnitBased,
                BillingPeriod = BillingPeriod.Monthly,
                CommissionPercent = (decimal?)null,
                AttributeSchemaJson = (string?)null,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("d2222222-2222-2222-2222-222222222222"),
                Code = "SRV-DED-1",
                Name = "سرور اختصاصی",
                CategoryId = Guid.Parse("c2222222-2222-2222-2222-222222222222"),
                ServiceType = ServiceType.DedicatedServer,
                Unit = ServiceUnit.Monthly,
                Description = "سرور اختصاصی ماهیانه",
                IsActive = true,
                PricingMode = PricingMode.UnitBased,
                BillingPeriod = BillingPeriod.Monthly,
                CommissionPercent = (decimal?)null,
                AttributeSchemaJson = (string?)null,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("d3333333-3333-3333-3333-333333333333"),
                Code = "RACK-42U",
                Name = "فضای رک ۴۲ یونیت",
                CategoryId = Guid.Parse("c2222222-2222-2222-2222-222222222222"),
                ServiceType = ServiceType.RackSpace,
                Unit = ServiceUnit.RackUnit,
                Description = "فضای رک در دیتاسنتر",
                IsActive = true,
                PricingMode = PricingMode.UnitBased,
                BillingPeriod = BillingPeriod.Monthly,
                CommissionPercent = (decimal?)null,
                AttributeSchemaJson = (string?)null,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            });
    }
}
