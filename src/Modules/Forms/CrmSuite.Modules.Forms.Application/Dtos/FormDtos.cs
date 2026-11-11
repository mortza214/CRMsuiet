using CrmSuite.Modules.Forms.Domain.Enums;

namespace CrmSuite.Modules.Forms.Application.Dtos;

public sealed record FormFieldDto(
    Guid Id,
    string Key,
    string Label,
    FormFieldType FieldType,
    bool IsRequired,
    string? DefaultValue,
    string? OptionsJson,
    string? MinValue,
    string? MaxValue,
    string? RegexPattern,
    int Order,
    string? VisibleIfJson);

public sealed record FormTemplateDto(
    Guid Id,
    string Code,
    string Title,
    string SchemaJson,
    int Version,
    bool IsActive,
    FormDefinitionStatus Status,
    FormEntityType EntityType,
    Guid? TargetServiceId,
    Guid? TargetServiceCategoryId,
    DateTime? PublishedAt,
    string? PublishedAtJalali,
    DateTime CreatedAt,
    string CreatedAtJalali,
    IReadOnlyList<FormFieldDto> Fields);

public sealed record FormSubmissionDto(
    Guid Id,
    Guid FormTemplateId,
    string? FormTitle,
    int FormVersion,
    FormEntityType EntityType,
    Guid? EntityId,
    Guid? CustomerId,
    string? CustomerName,
    Guid? OrderId,
    string SubmittedDataJson,
    FormSubmissionStatus Status,
    Guid SubmittedByUserId,
    DateTime SubmittedAt,
    string SubmittedAtJalali,
    DateTime CreatedAt,
    string CreatedAtJalali,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAt,
    string? ReviewedAtJalali,
    string? ReviewNote);
