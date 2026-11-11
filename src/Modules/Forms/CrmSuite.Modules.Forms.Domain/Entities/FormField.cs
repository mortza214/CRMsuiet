using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Forms.Domain.Enums;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Forms.Domain.Entities;

public sealed class FormField : BaseEntity
{
    private FormField()
    {
    }

    public FormField(
        Guid formDefinitionId,
        string key,
        string label,
        FormFieldType fieldType,
        bool isRequired,
        string? defaultValue,
        string? optionsJson,
        string? minValue,
        string? maxValue,
        string? regexPattern,
        int order,
        string? visibleIfJson)
        : base(Guid.NewGuid())
    {
        if (formDefinitionId == Guid.Empty)
        {
            throw new DomainException("فرم مربوط به فیلد معتبر نیست.", "FORMS_FIELD_DEFINITION_INVALID");
        }

        Key = Normalize(key, "کلید فیلد الزامی است.", "FORMS_FIELD_KEY_REQUIRED", 120);
        Label = Normalize(label, "عنوان فیلد الزامی است.", "FORMS_FIELD_LABEL_REQUIRED", 300);
        FormDefinitionId = formDefinitionId;
        FieldType = fieldType;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
        OptionsJson = optionsJson;
        MinValue = minValue;
        MaxValue = maxValue;
        RegexPattern = regexPattern;
        Order = order < 0 ? 0 : order;
        VisibleIfJson = visibleIfJson;
    }

    public Guid FormDefinitionId { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Label { get; private set; } = string.Empty;

    public FormFieldType FieldType { get; private set; }

    public bool IsRequired { get; private set; }

    public string? DefaultValue { get; private set; }

    public string? OptionsJson { get; private set; }

    public string? MinValue { get; private set; }

    public string? MaxValue { get; private set; }

    public string? RegexPattern { get; private set; }

    public int Order { get; private set; }

    public string? VisibleIfJson { get; private set; }

    private static string Normalize(string value, string message, string code, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(message, code);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"طول مقدار حداکثر {maxLength} کاراکتر است.", "FORMS_FIELD_TOO_LONG");
        }

        return normalized;
    }
}
