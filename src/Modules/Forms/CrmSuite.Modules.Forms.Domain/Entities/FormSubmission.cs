using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Forms.Domain.Enums;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Forms.Domain.Entities;

public sealed class FormSubmission : BaseEntity
{
    private FormSubmission()
    {
    }

    /// <summary>Legacy constructor preserved for existing tests and old callers.</summary>
    public FormSubmission(
        Guid formTemplateId,
        Guid customerId,
        Guid? orderId,
        string submittedDataJson,
        Guid submittedByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        Initialize(
            formTemplateId,
            1,
            FormEntityType.Customer,
            customerId,
            submittedDataJson,
            submittedByUserId,
            createdAtUtc);
        CustomerId = customerId;
        OrderId = orderId;
    }

    public FormSubmission(
        Guid formDefinitionId,
        int formVersion,
        FormEntityType entityType,
        Guid? entityId,
        string submittedDataJson,
        Guid submittedByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        Initialize(
            formDefinitionId,
            formVersion,
            entityType,
            entityId,
            submittedDataJson,
            submittedByUserId,
            createdAtUtc);
    }

    public Guid FormTemplateId { get; private set; }

    public Guid FormDefinitionId => FormTemplateId;

    public int FormVersion { get; private set; }

    public FormEntityType EntityType { get; private set; }

    public Guid? EntityId { get; private set; }

    /// <summary>Legacy column; only retained for backward-compatible reads.</summary>
    public Guid? CustomerId { get; private set; }

    /// <summary>Legacy column; only retained for backward-compatible reads.</summary>
    public Guid? OrderId { get; private set; }

    public string SubmittedDataJson { get; private set; } = string.Empty;

    public FormSubmissionStatus Status { get; private set; }

    public Guid SubmittedByUserId { get; private set; }

    public DateTime SubmittedAt { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTime? ReviewedAt { get; private set; }

    public string? ReviewNote { get; private set; }

    public ICollection<FormSubmissionItem> Items { get; private set; } = new List<FormSubmissionItem>();

    public void AddItem(FormSubmissionItem item)
    {
        if (item.FormSubmissionId != Id)
        {
            throw new DomainException(
                "آیتم پاسخ به این فرم تعلق ندارد.",
                "FORMS_SUBMISSION_ITEM_MISMATCH");
        }

        Items.Add(item);
    }

    public void UpdateData(string dataJson, Guid userId, DateTime utcNow, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(dataJson))
        {
            throw new DomainException("داده‌های پاسخ الزامی است.", "FORMS_SUBMISSION_DATA_REQUIRED");
        }

        SubmittedDataJson = dataJson;
        Status = FormSubmissionStatus.Submitted;
        SubmittedByUserId = userId;
        SubmittedAt = utcNow;
        ReviewedByUserId = null;
        ReviewedAt = null;
        ReviewNote = note;
        MarkUpdated(userId, utcNow);
    }

    public void Approve(Guid reviewerId, DateTime utcNow, string? note)
    {
        EnsureReviewable();
        Status = FormSubmissionStatus.Approved;
        ReviewedByUserId = reviewerId;
        ReviewedAt = utcNow;
        ReviewNote = note;
        MarkUpdated(reviewerId, utcNow);
    }

    public void Reject(Guid reviewerId, DateTime utcNow, string? note)
    {
        EnsureReviewable();
        Status = FormSubmissionStatus.Rejected;
        ReviewedByUserId = reviewerId;
        ReviewedAt = utcNow;
        ReviewNote = note;
        MarkUpdated(reviewerId, utcNow);
    }

    private void Initialize(
        Guid formTemplateId,
        int formVersion,
        FormEntityType entityType,
        Guid? entityId,
        string submittedDataJson,
        Guid submittedByUserId,
        DateTime createdAtUtc)
    {
        if (formTemplateId == Guid.Empty)
        {
            throw new DomainException("شناسه فرم معتبر نیست.", "FORMS_TEMPLATE_ID_INVALID");
        }

        if (string.IsNullOrWhiteSpace(submittedDataJson))
        {
            throw new DomainException("داده‌های پاسخ الزامی است.", "FORMS_SUBMISSION_DATA_REQUIRED");
        }

        if (entityType != FormEntityType.None && !entityId.HasValue)
        {
            throw new DomainException("شناسه موجودیت پاسخ الزامی است.", "FORMS_ENTITY_ID_REQUIRED");
        }

        FormTemplateId = formTemplateId;
        FormVersion = formVersion < 1 ? 1 : formVersion;
        EntityType = entityType;
        EntityId = entityId;
        SubmittedDataJson = submittedDataJson;
        Status = FormSubmissionStatus.Submitted;
        SubmittedByUserId = submittedByUserId;
        SubmittedAt = createdAtUtc;
        MarkCreated(submittedByUserId, createdAtUtc);
    }

    private void EnsureReviewable()
    {
        if (Status != FormSubmissionStatus.Submitted)
        {
            throw new DomainException(
                "فقط پاسخ ارسال‌شده قابل بررسی است.",
                "FORMS_SUBMISSION_NOT_REVIEWABLE");
        }
    }
}
