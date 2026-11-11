using CrmSuite.BuildingBlocks.Domain.Results;
using CrmSuite.Modules.Forms.Application.Abstractions;
using CrmSuite.Modules.Forms.Application.Dtos;
using CrmSuite.Modules.Forms.Application.Mapping;
using CrmSuite.Modules.Forms.Domain.Enums;
using MediatR;

namespace CrmSuite.Modules.Forms.Application.Queries;

public sealed class GetFormTemplatesQueryHandler
    : IRequestHandler<GetFormTemplatesQuery, Result<IReadOnlyList<FormTemplateDto>>>
{
    private readonly IFormTemplateRepository _templates;
    private readonly FormDtoMapper _mapper;

    public GetFormTemplatesQueryHandler(IFormTemplateRepository templates, FormDtoMapper mapper)
    {
        _templates = templates;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<FormTemplateDto>>> Handle(
        GetFormTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var templates = await _templates.GetAllAsync(request.IncludeInactive, cancellationToken);
        return Result.Success<IReadOnlyList<FormTemplateDto>>(
            templates.Select(_mapper.ToTemplateDto).ToArray());
    }
}

public sealed class GetFormTemplateByIdQueryHandler
    : IRequestHandler<GetFormTemplateByIdQuery, Result<FormTemplateDto>>
{
    private readonly IFormTemplateRepository _templates;
    private readonly FormDtoMapper _mapper;

    public GetFormTemplateByIdQueryHandler(IFormTemplateRepository templates, FormDtoMapper mapper)
    {
        _templates = templates;
        _mapper = mapper;
    }

    public async Task<Result<FormTemplateDto>> Handle(
        GetFormTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _templates.GetByIdWithFieldsAsync(request.Id, cancellationToken);
        return template is null
            ? Result.Failure<FormTemplateDto>("فرم یافت نشد.", "FORMS_TEMPLATE_NOT_FOUND")
            : Result.Success(_mapper.ToTemplateDto(template));
    }
}

public sealed class GetFormTemplateByCodeQueryHandler
    : IRequestHandler<GetFormTemplateByCodeQuery, Result<FormTemplateDto>>
{
    private readonly IFormTemplateRepository _templates;
    private readonly FormDtoMapper _mapper;

    public GetFormTemplateByCodeQueryHandler(IFormTemplateRepository templates, FormDtoMapper mapper)
    {
        _templates = templates;
        _mapper = mapper;
    }

    public async Task<Result<FormTemplateDto>> Handle(
        GetFormTemplateByCodeQuery request,
        CancellationToken cancellationToken)
    {
        var template = await _templates.GetByCodeAsync(request.Code, cancellationToken);
        if (template is null)
        {
            return Result.Failure<FormTemplateDto>("فرم یافت نشد.", "FORMS_TEMPLATE_NOT_FOUND");
        }

        if (request.RequirePublished && template.Status != FormDefinitionStatus.Published)
        {
            return Result.Failure<FormTemplateDto>("فرم منتشر نشده است.", "FORMS_TEMPLATE_NOT_PUBLISHED");
        }

        return Result.Success(_mapper.ToTemplateDto(template));
    }
}

public sealed class GetFormSubmissionsQueryHandler
    : IRequestHandler<GetFormSubmissionsQuery, Result<PagedResult<FormSubmissionDto>>>
{
    private readonly IFormSubmissionReadRepository _readRepository;

    public GetFormSubmissionsQueryHandler(IFormSubmissionReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<PagedResult<FormSubmissionDto>>> Handle(
        GetFormSubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await _readRepository.SearchAsync(request, cancellationToken);
        return Result.Success(page);
    }
}

public sealed class GetFormSubmissionHistoryQueryHandler
    : IRequestHandler<GetFormSubmissionHistoryQuery, Result<IReadOnlyList<FormSubmissionHistoryDto>>>
{
    private readonly IFormSubmissionRepository _submissions;
    private readonly CrmSuite.Modules.Foundation.Application.Abstractions.IJalaliDateService _jalali;

    public GetFormSubmissionHistoryQueryHandler(
        IFormSubmissionRepository submissions,
        CrmSuite.Modules.Foundation.Application.Abstractions.IJalaliDateService jalali)
    {
        _submissions = submissions;
        _jalali = jalali;
    }

    public async Task<Result<IReadOnlyList<FormSubmissionHistoryDto>>> Handle(
        GetFormSubmissionHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _submissions.GetHistoryAsync(request.SubmissionId, cancellationToken);
        return Result.Success<IReadOnlyList<FormSubmissionHistoryDto>>(
            items.Select(item => new FormSubmissionHistoryDto(
                item.Id,
                item.Action,
                item.ModifiedBy,
                item.ModifiedAt,
                _jalali.ToJalali(item.ModifiedAt),
                item.PreviousDataJson)).ToArray());
    }
}
