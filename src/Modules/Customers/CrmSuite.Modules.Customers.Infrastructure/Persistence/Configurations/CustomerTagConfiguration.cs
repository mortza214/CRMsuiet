using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Customers.Infrastructure.Persistence.Configurations;

public sealed class CustomerTagConfiguration : BaseEntityConfiguration<CustomerTag>
{
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected override void ConfigureEntity(EntityTypeBuilder<CustomerTag> builder)
    {
        builder.ToTable("customer_tags");

        builder.Property(tag => tag.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(tag => tag.Name)
            .IsUnique()
            .HasDatabaseName("ux_customer_tags_name");

        builder.Property(tag => tag.Color)
            .HasColumnName("color")
            .HasMaxLength(7)
            .IsRequired();

        builder.HasData(
            new
            {
                Id = Guid.Parse("b1111111-1111-1111-1111-111111111111"),
                Name = "مشتری وفادار",
                Color = "#52c41a",
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("b2222222-2222-2222-2222-222222222222"),
                Name = "بدهکار",
                Color = "#ff4d4f",
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            });
    }
}
