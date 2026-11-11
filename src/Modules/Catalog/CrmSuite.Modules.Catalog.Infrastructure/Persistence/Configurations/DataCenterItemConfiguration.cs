using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Catalog.Domain.Enums;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class DataCenterItemConfiguration : BaseEntityConfiguration<DataCenterItem>
{
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected override void ConfigureEntity(EntityTypeBuilder<DataCenterItem> builder)
    {
        builder.ToTable("data_center_items");

        builder.Property(item => item.ItemType)
            .HasColumnName("item_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(item => item.Name)
            .IsUnique()
            .HasDatabaseName("ux_data_center_items_name");

        builder.Property(item => item.Price)
            .HasColumnName("price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.Unit)
            .HasColumnName("unit")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasData(
            new
            {
                Id = Guid.Parse("a4444444-4444-4444-4444-444444444444"),
                ItemType = DataCenterItemType.Power,
                Name = "برق ۱ آمپر",
                Price = 100000m,
                Unit = ServiceUnit.Monthly,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("a5555555-5555-5555-5555-555555555555"),
                ItemType = DataCenterItemType.IP,
                Name = "IP اضافه",
                Price = 50000m,
                Unit = ServiceUnit.Piece,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            });
    }
}
