using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Forms.Infrastructure.Persistence.Configurations;

public sealed class FormSubmissionHistoryConfiguration : BaseEntityConfiguration<FormSubmissionHistory>
{
    protected override void ConfigureEntity(EntityTypeBuilder<FormSubmissionHistory> builder)
    {
        builder.ToTable("form_submission_history");
        builder.Property(history => history.FormSubmissionId).HasColumnName("form_submission_id").IsRequired();
        builder.Property(history => history.Action).HasColumnName("action").HasMaxLength(30).IsRequired();
        builder.Property(history => history.ModifiedBy).HasColumnName("modified_by").IsRequired();
        builder.Property(history => history.ModifiedAt).HasColumnName("modified_at").HasColumnType("datetime(6)").IsRequired();
        builder.Property(history => history.PreviousDataJson).HasColumnName("previous_data_json").HasColumnType("longtext");
        builder.HasIndex(history => history.FormSubmissionId).HasDatabaseName("ix_form_submission_history_submission");
    }
}
