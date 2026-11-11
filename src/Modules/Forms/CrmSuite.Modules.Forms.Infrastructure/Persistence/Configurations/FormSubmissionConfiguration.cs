using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Forms.Infrastructure.Persistence.Configurations;

public sealed class FormSubmissionConfiguration : BaseEntityConfiguration<FormSubmission>
{
    protected override void ConfigureEntity(EntityTypeBuilder<FormSubmission> builder)
    {
        builder.ToTable("form_submissions");

        builder.Property(submission => submission.FormTemplateId)
            .HasColumnName("form_template_id")
            .IsRequired();
        builder.Property(submission => submission.FormVersion)
            .HasColumnName("form_version")
            .IsRequired();
        builder.Property(submission => submission.EntityType)
            .HasColumnName("entity_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(submission => submission.EntityId)
            .HasColumnName("entity_id");

        // Legacy columns are intentionally retained and nullable. New submissions do not write them.
        builder.Property(submission => submission.CustomerId)
            .HasColumnName("customer_id");
        builder.Property(submission => submission.OrderId)
            .HasColumnName("order_id");

        builder.Property(submission => submission.SubmittedDataJson)
            .HasColumnName("submitted_data_json")
            .HasColumnType("longtext")
            .IsRequired();
        builder.Property(submission => submission.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(submission => submission.SubmittedByUserId)
            .HasColumnName("submitted_by_user_id")
            .IsRequired();
        builder.Property(submission => submission.SubmittedAt)
            .HasColumnName("submitted_at")
            .HasColumnType("datetime(6)")
            .IsRequired();
        builder.Property(submission => submission.ReviewedByUserId)
            .HasColumnName("reviewed_by_user_id");
        builder.Property(submission => submission.ReviewedAt)
            .HasColumnName("reviewed_at")
            .HasColumnType("datetime(6)");
        builder.Property(submission => submission.ReviewNote)
            .HasColumnName("review_note")
            .HasMaxLength(2000);

        builder.HasIndex(submission => new { submission.FormTemplateId, submission.CustomerId })
            .HasDatabaseName("ix_form_submissions_template_customer");
        builder.HasIndex(submission => new { submission.EntityType, submission.EntityId })
            .HasDatabaseName("ix_form_submissions_entity");
        builder.HasIndex(submission => new { submission.FormTemplateId, submission.Status })
            .HasDatabaseName("ix_form_submissions_template_status");
        builder.HasIndex(submission => submission.SubmittedAt)
            .HasDatabaseName("ix_form_submissions_submitted_at");

        builder.HasMany(submission => submission.Items)
            .WithOne()
            .HasForeignKey(item => item.FormSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
