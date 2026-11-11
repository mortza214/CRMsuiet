using CrmSuite.Modules.Customers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Customers.Infrastructure.Persistence.Configurations;

public sealed class CustomerTagMapConfiguration : IEntityTypeConfiguration<CustomerTagMap>
{
    public void Configure(EntityTypeBuilder<CustomerTagMap> builder)
    {
        builder.ToTable("customer_tag_maps");

        builder.HasKey(map => new { map.CustomerId, map.TagId });

        builder.Property(map => map.CustomerId).HasColumnName("customer_id");
        builder.Property(map => map.TagId).HasColumnName("tag_id");

        builder.HasOne(map => map.Customer)
            .WithMany(customer => customer.TagMaps)
            .HasForeignKey(map => map.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(map => map.Tag)
            .WithMany()
            .HasForeignKey(map => map.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(map => map.TagId)
            .HasDatabaseName("ix_customer_tag_maps_tag_id");

        builder.HasQueryFilter(map => !map.Customer.IsDeleted && !map.Tag.IsDeleted);
    }
}
