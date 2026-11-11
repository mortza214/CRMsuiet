using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Forms.Domain.Enums;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Forms.Domain.Entities;

public sealed class FormSubmissionItem : BaseEntity
{
    private FormSubmissionItem()
    {
    }

    public FormSubmissionItem(
        Guid formSubmissionId,
        string fieldKey,
        FormFieldType fieldType,
        string? valueText,
        decimal? valueNumber)
        : base(Guid.NewGuid())
    {
        if (formSubmissionId == Guid.Empty)
        {
            throw new DomainException("پاسخ فرم معتبر نیست.", "FORMS_SUBMISSION_ITEM_INVALID");
        }

        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            throw new DomainException("کلید فیلد پاسخ الزامی است.", "FORMS_SUBMISSION_ITEM_KEY_REQUIRED");
        }

        FormSubmissionId = formSubmissionId;
        FieldKey = fieldKey.Trim();
        FieldType = fieldType;
        ValueText = valueText;
        ValueNumber = valueNumber;
    }

    public Guid FormSubmissionId { get; private set; }

    public string FieldKey { get; private set; } = string.Empty;

    public FormFieldType FieldType { get; private set; }

    public string? ValueText { get; private set; }

    public decimal? ValueNumber { get; private set; }
}
