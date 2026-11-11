using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Forms.Domain.Entities;

public sealed class FormSubmissionHistory : BaseEntity
{
    private FormSubmissionHistory()
    {
    }

    public FormSubmissionHistory(
        Guid formSubmissionId,
        string action,
        Guid modifiedBy,
        DateTime modifiedAtUtc,
        string? previousDataJson)
        : base(Guid.NewGuid())
    {
        FormSubmissionId = formSubmissionId;
        Action = string.IsNullOrWhiteSpace(action) ? "Updated" : action.Trim();
        ModifiedBy = modifiedBy;
        ModifiedAt = DateTime.SpecifyKind(modifiedAtUtc, DateTimeKind.Utc);
        PreviousDataJson = previousDataJson;
        MarkCreated(modifiedBy, modifiedAtUtc);
    }

    public Guid FormSubmissionId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public Guid ModifiedBy { get; private set; }

    public DateTime ModifiedAt { get; private set; }

    public string? PreviousDataJson { get; private set; }
}
