using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Forms.Application.Dtos;
using CrmSuite.Modules.Forms.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Forms.Application.Commands;

public sealed record CreateFormTemplateCommand(
    string Title,
    string SchemaJson,
    int Version = 1,
    bool IsActive = true,
    string? Code = null,
    FormEntityType EntityType = FormEntityType.None,
    Guid? TargetServiceId = null,
    Guid? TargetServiceCategoryId = null) : IRequest<Result<FormTemplateDto>>;

public sealed record UpdateFormTemplateCommand(
    Guid Id,
    string Code,
    string Title,
    string SchemaJson,
    FormEntityType EntityType,
    Guid? TargetServiceId,
    Guid? TargetServiceCategoryId) : IRequest<Result<FormTemplateDto>>;

public sealed record PublishFormTemplateCommand(Guid Id) : IRequest<Result<FormTemplateDto>>;

public sealed record ArchiveFormTemplateCommand(Guid Id) : IRequest<Result<FormTemplateDto>>;

public sealed record SubmitFormCommand(
    Guid FormTemplateId,
    Guid? CustomerId,
    Guid? OrderId,
    string SubmittedDataJson,
    bool Portal = false,
    FormEntityType? EntityType = null,
    Guid? EntityId = null) : IRequest<Result<FormSubmissionDto>>;

public sealed record ReviewFormSubmissionCommand(
    Guid SubmissionId,
    bool Approve,
    string? Note) : IRequest<Result<FormSubmissionDto>>;
