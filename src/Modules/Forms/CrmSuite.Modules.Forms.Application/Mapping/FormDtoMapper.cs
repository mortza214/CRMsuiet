using System.Diagnostics.CodeAnalysis;
using CrmSuite.Modules.Forms.Application.Dtos;
using CrmSuite.Modules.Forms.Domain.Entities;
using CrmSuite.Modules.Forms.Domain.Enums;
using CrmSuite.Modules.Foundation.Application.Abstractions;

namespace CrmSuite.Modules.Forms.Application.Mapping;

[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Projection methods share the mapper instance for DI consistency.")]
public sealed class FormDtoMapper
{
    private readonly IJalaliDateService _jalaliDate;

    public FormDtoMapper(IJalaliDateService jalaliDate)
    {
        _jalaliDate = jalaliDate;
    }

    public FormTemplateDto ToTemplateDto(FormTemplate template)
    {
        var createdAt = AsUtc(template.CreatedAt);
        var publishedAt = template.PublishedAt.HasValue
            ? AsUtc(template.PublishedAt.Value)
            : (DateTime?)null;

        return new FormTemplateDto(
            template.Id,
            template.Code,
            template.Title,
            template.SchemaJson,
            template.Version,
            template.IsActive,
            template.Status,
            template.EntityType,
            template.TargetServiceId,
            template.TargetServiceCategoryId,
            publishedAt,
            publishedAt.HasValue ? _jalaliDate.ToJalali(publishedAt.Value) : null,
            createdAt,
            _jalaliDate.ToJalali(createdAt),
            template.Fields
                .OrderBy(field => field.Order)
                .Select(ToFieldDto)
                .ToArray());
    }

    public FormFieldDto ToFieldDto(FormField field) =>
        new(
            field.Id,
            field.Key,
            field.Label,
            field.FieldType,
            field.IsRequired,
            field.DefaultValue,
            field.OptionsJson,
            field.MinValue,
            field.MaxValue,
            field.RegexPattern,
            field.Order,
            field.VisibleIfJson);

    public FormSubmissionDto ToSubmissionDto(
        FormSubmission submission,
        string? formTitle,
        string? customerName)
    {
        var createdAt = AsUtc(submission.CreatedAt);
        var submittedAt = submission.SubmittedAt == default
            ? createdAt
            : AsUtc(submission.SubmittedAt);
        var reviewedAt = submission.ReviewedAt.HasValue
            ? AsUtc(submission.ReviewedAt.Value)
            : (DateTime?)null;
        var customerId = submission.CustomerId
                         ?? (submission.EntityType == FormEntityType.Customer
                             ? submission.EntityId
                             : null);
        var orderId = submission.OrderId
                      ?? (submission.EntityType == FormEntityType.Order
                          ? submission.EntityId
                          : null);

        return new FormSubmissionDto(
            submission.Id,
            submission.FormTemplateId,
            formTitle,
            submission.FormVersion,
            submission.EntityType,
            submission.EntityId,
            customerId,
            customerName,
            orderId,
            submission.SubmittedDataJson,
            submission.Status,
            submission.SubmittedByUserId,
            submittedAt,
            _jalaliDate.ToJalali(submittedAt),
            createdAt,
            _jalaliDate.ToJalali(createdAt),
            submission.ReviewedByUserId,
            reviewedAt,
            reviewedAt.HasValue ? _jalaliDate.ToJalali(reviewedAt.Value) : null,
            submission.ReviewNote);
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
