using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Customers.Infrastructure.Persistence.Configurations;

public sealed class CustomerGroupConfiguration : BaseEntityConfiguration<CustomerGroup>
{
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected override void ConfigureEntity(EntityTypeBuilder<CustomerGroup> builder)
    {
        builder.ToTable("customer_groups");

        builder.Property(group => group.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(group => group.Name)
            .IsUnique()
            .HasDatabaseName("ux_customer_groups_name");

        builder.Property(group => group.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.HasData(
            new
            {
                Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                Name = "عادی",
                Description = "گروه پیش‌فرض مشتریان",
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
                Name = "ویژه",
                Description = "مشتریان ویژه",
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("a3333333-3333-3333-3333-333333333333"),
                Name = "ریسلر",
                Description = "گروه مشتریان ریسلر",
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            });
    }
}
