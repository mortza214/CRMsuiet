using CrmSuite.Modules.Forms.Domain.Enums;

namespace CrmSuite.Modules.Forms.Api.Contracts;

public sealed class CreateFormTemplateRequest
{
    public string? Code { get; init; }

    public string Title { get; init; } = string.Empty;

    public string SchemaJson { get; init; } = string.Empty;

    public int Version { get; init; } = 1;

    public bool IsActive { get; init; } = true;

    public FormEntityType EntityType { get; init; } = FormEntityType.None;

    public Guid? TargetServiceId { get; init; }

    public Guid? TargetServiceCategoryId { get; init; }
}

public sealed class UpdateFormTemplateRequest
{
    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string SchemaJson { get; init; } = string.Empty;

    public FormEntityType EntityType { get; init; }

    public Guid? TargetServiceId { get; init; }

    public Guid? TargetServiceCategoryId { get; init; }
}

public sealed class SubmitFormRequest
{
    public Guid FormTemplateId { get; init; }

    /// <summary>Legacy alias; converted to EntityType=Customer when supplied.</summary>
    public Guid? CustomerId { get; init; }

    /// <summary>Legacy alias; converted to EntityType=Order when supplied.</summary>
    public Guid? OrderId { get; init; }

    public FormEntityType? EntityType { get; init; }

    public Guid? EntityId { get; init; }

    public string SubmittedDataJson { get; init; } = string.Empty;
}

public sealed class ReviewFormSubmissionRequest
{
    public bool Approve { get; init; }

    public string? Note { get; init; }
}
