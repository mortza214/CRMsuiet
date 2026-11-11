using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class ServiceCategoryConfiguration : BaseEntityConfiguration<ServiceCategory>
{
    private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    protected override void ConfigureEntity(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("service_categories");

        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(category => category.ParentId)
            .HasColumnName("parent_id");

        builder.HasOne<ServiceCategory>()
            .WithMany()
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(category => new { category.ParentId, category.Name })
            .IsUnique()
            .HasDatabaseName("ux_service_categories_parent_name");

        builder.HasData(
            new
            {
                Id = Guid.Parse("c1111111-1111-1111-1111-111111111111"),
                Name = "اینترنت",
                ParentId = (Guid?)null,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("c2222222-2222-2222-2222-222222222222"),
                Name = "دیتاسنتر",
                ParentId = (Guid?)null,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            },
            new
            {
                Id = Guid.Parse("c3333333-3333-3333-3333-333333333333"),
                Name = "تجهیزات",
                ParentId = (Guid?)null,
                CreatedAt = SeedTimestamp,
                IsDeleted = false,
            });
    }
}
