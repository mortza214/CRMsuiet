using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Forms.Infrastructure.Persistence.Configurations;

public sealed class FormFieldConfiguration : BaseEntityConfiguration<FormField>
{
    protected override void ConfigureEntity(EntityTypeBuilder<FormField> builder)
    {
        builder.ToTable("form_fields");
        builder.Property(field => field.FormDefinitionId)
            .HasColumnName("form_definition_id")
            .IsRequired();
        builder.Property(field => field.Key)
            .HasColumnName("field_key")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(field => field.Label)
            .HasColumnName("label")
            .HasMaxLength(300)
            .IsRequired();
        builder.Property(field => field.FieldType)
            .HasColumnName("field_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(field => field.IsRequired).HasColumnName("is_required").IsRequired();
        builder.Property(field => field.DefaultValue).HasColumnName("default_value").HasMaxLength(2000);
        builder.Property(field => field.OptionsJson).HasColumnName("options_json").HasColumnType("longtext");
        builder.Property(field => field.MinValue).HasColumnName("min_value").HasMaxLength(100);
        builder.Property(field => field.MaxValue).HasColumnName("max_value").HasMaxLength(100);
        builder.Property(field => field.RegexPattern).HasColumnName("regex_pattern").HasMaxLength(500);
        builder.Property(field => field.Order).HasColumnName("sort_order").IsRequired();
        builder.Property(field => field.VisibleIfJson).HasColumnName("visible_if_json").HasColumnType("longtext");
        builder.HasIndex(field => new { field.FormDefinitionId, field.Key })
            .IsUnique()
            .HasDatabaseName("ux_form_fields_definition_key");
        builder.HasIndex(field => new { field.FormDefinitionId, field.Order })
            .HasDatabaseName("ix_form_fields_definition_order");
    }
}
