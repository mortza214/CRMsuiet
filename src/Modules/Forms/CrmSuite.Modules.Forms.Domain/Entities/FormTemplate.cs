using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Forms.Domain.Enums;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Forms.Domain.Entities;

/// <summary>
/// The persisted form definition. The legacy type name is intentionally kept because
/// existing clients and the current table map to it, while the public model is a
/// versioned, publishable definition.
/// </summary>
public sealed class FormTemplate : BaseEntity
{
    private FormTemplate()
    {
    }

    public FormTemplate(
        string title,
        string schemaJson,
        int version,
        bool isActive,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        var status = isActive ? FormDefinitionStatus.Published : FormDefinitionStatus.Draft;
        Initialize(
            $"FRM-{Id:N}"[..16].ToUpperInvariant(),
            title,
            FormEntityType.None,
            null,
            null,
            schemaJson,
            version,
            status,
            createdByUserId,
            createdAtUtc);
    }

    public FormTemplate(
        string code,
        string title,
        FormEntityType entityType,
        Guid? targetServiceId,
        Guid? targetServiceCategoryId,
        string schemaJson,
        int version,
        FormDefinitionStatus status,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        Initialize(
            code,
            title,
            entityType,
            targetServiceId,
            targetServiceCategoryId,
            schemaJson,
            version,
            status,
            createdByUserId,
            createdAtUtc);
    }

    public string Code { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public FormEntityType EntityType { get; private set; }

    public Guid? TargetServiceId { get; private set; }

    public Guid? TargetServiceCategoryId { get; private set; }

    public FormDefinitionStatus Status { get; private set; }

    public int Version { get; private set; }

    public string SchemaJson { get; private set; } = string.Empty;

    public DateTime? PublishedAt { get; private set; }

    public DateTime? ArchivedAt { get; private set; }

    /// <summary>Legacy column kept in sync for older read models and existing databases.</summary>
    public bool LegacyIsActive { get; private set; }

    public bool IsActive => Status == FormDefinitionStatus.Published;

    public ICollection<FormField> Fields { get; private set; } = new List<FormField>();

    public void UpdateDraft(
        string code,
        string title,
        FormEntityType entityType,
        Guid? targetServiceId,
        Guid? targetServiceCategoryId,
        string schemaJson,
        Guid? userId,
        DateTime utcNow)
    {
        EnsureDraft();
        Code = NormalizeCode(code);
        Title = NormalizeTitle(title);
        SchemaJson = NormalizeSchema(schemaJson);
        EntityType = entityType;
        TargetServiceId = targetServiceId;
        TargetServiceCategoryId = targetServiceCategoryId;
        MarkUpdated(userId, utcNow);
    }

    public void Publish(IReadOnlyCollection<FormField> fields, Guid? userId, DateTime utcNow)
    {
        EnsureDraft();

        if (fields.Count == 0)
        {
            throw new DomainException(
                "فرم برای انتشار حداقل به یک فیلد نیاز دارد.",
                "FORMS_FIELDS_REQUIRED");
        }

        foreach (var field in fields)
        {
            if (field.FormDefinitionId != Id)
            {
                throw new DomainException(
                    "فیلد به تعریف فرم جاری تعلق ندارد.",
                    "FORMS_FIELD_DEFINITION_MISMATCH");
            }
        }

        Fields.Clear();
        foreach (var field in fields)
        {
            Fields.Add(field);
        }

        Status = FormDefinitionStatus.Published;
        LegacyIsActive = true;
        Version = Version < 1 ? 1 : Version + 1;
        PublishedAt = utcNow;
        ArchivedAt = null;
        MarkUpdated(userId, utcNow);
    }

    public void Archive(Guid? userId, DateTime utcNow)
    {
        if (Status == FormDefinitionStatus.Archived)
        {
            throw new DomainException("فرم قبلاً آرشیو شده است.", "FORMS_ALREADY_ARCHIVED");
        }

        Status = FormDefinitionStatus.Archived;
        LegacyIsActive = false;
        ArchivedAt = utcNow;
        MarkUpdated(userId, utcNow);
    }

    public FormField? FindField(string key) =>
        Fields.FirstOrDefault(field =>
            string.Equals(field.Key, key, StringComparison.OrdinalIgnoreCase));

    private void Initialize(
        string code,
        string title,
        FormEntityType entityType,
        Guid? targetServiceId,
        Guid? targetServiceCategoryId,
        string schemaJson,
        int version,
        FormDefinitionStatus status,
        Guid? createdByUserId,
        DateTime createdAtUtc)
    {
        Code = NormalizeCode(code);
        Title = NormalizeTitle(title);
        SchemaJson = NormalizeSchema(schemaJson);
        EntityType = entityType;
        TargetServiceId = targetServiceId;
        TargetServiceCategoryId = targetServiceCategoryId;
        Status = status;
        Version = version < 1 ? 1 : version;
        LegacyIsActive = status == FormDefinitionStatus.Published;
        PublishedAt = status == FormDefinitionStatus.Published ? createdAtUtc : null;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    private void EnsureDraft()
    {
        if (Status != FormDefinitionStatus.Draft)
        {
            throw new DomainException(
                "فقط فرم پیش‌نویس قابل ویرایش یا انتشار است.",
                "FORMS_DEFINITION_NOT_DRAFT");
        }
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("کد فرم الزامی است.", "FORMS_CODE_REQUIRED");
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > 64)
        {
            throw new DomainException("کد فرم حداکثر ۶۴ کاراکتر است.", "FORMS_CODE_TOO_LONG");
        }

        return normalized;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("عنوان فرم الزامی است.", "FORMS_TITLE_REQUIRED");
        }

        var normalized = title.Trim();
        if (normalized.Length > 200)
        {
            throw new DomainException("عنوان فرم حداکثر ۲۰۰ کاراکتر است.", "FORMS_TITLE_TOO_LONG");
        }

        return normalized;
    }

    private static string NormalizeSchema(string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            throw new DomainException("Schema فرم الزامی است.", "FORMS_SCHEMA_REQUIRED");
        }

        return schemaJson;
    }
}
