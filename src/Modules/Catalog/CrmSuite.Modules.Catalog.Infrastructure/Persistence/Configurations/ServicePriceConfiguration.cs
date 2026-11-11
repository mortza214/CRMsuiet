using CrmSuite.Modules.Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class ServicePriceConfiguration : IEntityTypeConfiguration<ServicePrice>
{
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<ServicePrice> builder)
    {
        builder.ToTable("service_prices");

        builder.HasKey(price => price.Id);
        builder.Property(price => price.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(price => price.ServiceId)
            .HasColumnName("service_id")
            .IsRequired();

        builder.Property(price => price.Price)
            .HasColumnName("price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(price => price.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(price => price.EffectiveTo)
            .HasColumnName("effective_to")
            .HasColumnType("datetime(6)");

        builder.Property(price => price.Currency)
            .HasColumnName("currency")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(price => price.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasIndex(price => new { price.ServiceId, price.EffectiveFrom })
            .HasDatabaseName("ix_service_prices_service_effective");

        builder.HasIndex(price => new { price.ServiceId, price.EffectiveTo })
            .HasDatabaseName("ix_service_prices_service_open");

        builder.HasData(
            new
            {
                Id = Guid.Parse("e1111111-1111-1111-1111-111111111111"),
                ServiceId = Guid.Parse("d1111111-1111-1111-1111-111111111111"),
                Price = 500000m,
                EffectiveFrom = SeedTimestamp,
                EffectiveTo = (DateTime?)null,
                Currency = "IRR",
                CreatedAt = SeedTimestamp,
            },
            new
            {
                Id = Guid.Parse("e2222222-2222-2222-2222-222222222222"),
                ServiceId = Guid.Parse("d2222222-2222-2222-2222-222222222222"),
                Price = 5000000m,
                EffectiveFrom = SeedTimestamp,
                EffectiveTo = (DateTime?)null,
                Currency = "IRR",
                CreatedAt = SeedTimestamp,
            },
            new
            {
                Id = Guid.Parse("e3333333-3333-3333-3333-333333333333"),
                ServiceId = Guid.Parse("d3333333-3333-3333-3333-333333333333"),
                Price = 3000000m,
                EffectiveFrom = SeedTimestamp,
                EffectiveTo = (DateTime?)null,
                Currency = "IRR",
                CreatedAt = SeedTimestamp,
            });
    }
}
