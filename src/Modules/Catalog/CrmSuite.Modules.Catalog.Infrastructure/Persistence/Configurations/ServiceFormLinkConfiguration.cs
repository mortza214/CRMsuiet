using CrmSuite.Modules.Catalog.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class ServiceFormLinkConfiguration : BaseEntityConfiguration<ServiceFormLink>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ServiceFormLink> builder)
    {
        builder.ToTable("service_form_links");
        builder.Property(link => link.ServiceId)
            .HasColumnName("service_id")
            .IsRequired();
        builder.Property(link => link.FormDefinitionId)
            .HasColumnName("form_definition_id")
            .IsRequired();
        builder.Property(link => link.IsRequired)
            .HasColumnName("is_required")
            .IsRequired();
        builder.Property(link => link.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.HasIndex(link => new { link.ServiceId, link.FormDefinitionId })
            .IsUnique()
            .HasDatabaseName("ux_service_form_links_service_form");
        builder.HasIndex(link => link.ServiceId)
            .HasDatabaseName("ix_service_form_links_service");

        builder.HasOne<Service>()
            .WithMany()
            .HasForeignKey(link => link.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
