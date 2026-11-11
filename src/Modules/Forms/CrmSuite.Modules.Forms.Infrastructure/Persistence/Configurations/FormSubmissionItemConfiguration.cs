using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Forms.Infrastructure.Persistence.Configurations;

public sealed class FormSubmissionItemConfiguration : BaseEntityConfiguration<FormSubmissionItem>
{
    protected override void ConfigureEntity(EntityTypeBuilder<FormSubmissionItem> builder)
    {
        builder.ToTable("form_submission_items");
        builder.Property(item => item.FormSubmissionId)
            .HasColumnName("form_submission_id")
            .IsRequired();
        builder.Property(item => item.FieldKey)
            .HasColumnName("field_key")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(item => item.FieldType)
            .HasColumnName("field_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(item => item.ValueText)
            .HasColumnName("value_text")
            .HasColumnType("longtext");
        builder.Property(item => item.ValueNumber)
            .HasColumnName("value_number")
            .HasPrecision(18, 4);
        builder.HasIndex(item => new { item.FormSubmissionId, item.FieldKey })
            .HasDatabaseName("ix_form_submission_items_submission_key");
        builder.HasIndex(item => new { item.FieldKey, item.ValueNumber })
            .HasDatabaseName("ix_form_submission_items_key_number");
    }
}
