using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Forms.Infrastructure.Persistence.Configurations;

public sealed class FormTemplateConfiguration : BaseEntityConfiguration<FormTemplate>
{
    protected override void ConfigureEntity(EntityTypeBuilder<FormTemplate> builder)
    {
        builder.ToTable("form_templates");

        builder.Property(template => template.Code)
            .HasColumnName("code")
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex(template => template.Code)
            .IsUnique()
            .HasDatabaseName("ux_form_templates_code");

        builder.Property(template => template.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(template => template.SchemaJson)
            .HasColumnName("schema_json")
            .HasColumnType("longtext")
            .IsRequired();

        builder.Property(template => template.Version)
            .HasColumnName("version")
            .IsRequired();

        builder.Property(template => template.EntityType)
            .HasColumnName("entity_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(template => template.TargetServiceId)
            .HasColumnName("target_service_id");
        builder.Property(template => template.TargetServiceCategoryId)
            .HasColumnName("target_service_category_id");

        builder.Property(template => template.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(template => template.PublishedAt)
            .HasColumnName("published_at")
            .HasColumnType("datetime(6)");
        builder.Property(template => template.ArchivedAt)
            .HasColumnName("archived_at")
            .HasColumnType("datetime(6)");

        // Retained only so older databases/read paths can keep the original column.
        builder.Property(template => template.LegacyIsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Ignore(template => template.IsActive);

        builder.HasIndex(template => new { template.Title, template.Version })
            .HasDatabaseName("ix_form_templates_title_version");
        builder.HasIndex(template => new { template.EntityType, template.Status })
            .HasDatabaseName("ix_form_templates_entity_status");
        builder.HasIndex(template => template.TargetServiceId)
            .HasDatabaseName("ix_form_templates_target_service");
        builder.HasIndex(template => template.TargetServiceCategoryId)
            .HasDatabaseName("ix_form_templates_target_category");

        builder.HasMany(template => template.Fields)
            .WithOne()
            .HasForeignKey(field => field.FormDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
