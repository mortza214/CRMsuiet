using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Forms.Application.Dtos;
using CrmSuite.Modules.Forms.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Forms.Application.Queries;

public sealed record GetFormTemplatesQuery(bool IncludeInactive = false)
    : IRequest<Result<IReadOnlyList<FormTemplateDto>>>;

public sealed record GetFormTemplateByIdQuery(Guid Id) : IRequest<Result<FormTemplateDto>>;

public sealed record GetFormTemplateByCodeQuery(string Code, bool RequirePublished = true)
    : IRequest<Result<FormTemplateDto>>;

public sealed record GetFormSubmissionsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? FormTemplateId = null,
    Guid? CustomerId = null,
    FormEntityType? EntityType = null,
    Guid? EntityId = null,
    FormSubmissionStatus? Status = null) : IRequest<Result<PagedResult<FormSubmissionDto>>>;

public sealed record GetFormSubmissionHistoryQuery(Guid SubmissionId)
    : IRequest<Result<IReadOnlyList<FormSubmissionHistoryDto>>>;
